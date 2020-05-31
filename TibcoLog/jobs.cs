using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using StringMatch;

namespace TibcoLog
{
    /// <summary>
    /// cache to handle last X lines;
    /// </summary>
    class jobCache
    {
        int _size; //cache size
        string[] lines; //cache
        int index = -1; //index in cache - used to loop through lines
        int fileRowNum = -1; //absolute index

        public int size
        {
            get { return _size; }
            set { _size = value; }
        }

        /// <summary>
        /// add a new line to buffer
        /// </summary>
        /// <param name="line">text to add</param>
        public void addRow(string line)
        {
            index = (index + 1) % size;
            lines[index] = line;

        }

        /// <summary>
        /// lowest availabel rownum in cache
        /// </summary>
        public int minAvail
        {
            get { return Math.Max(fileRowNum - size + 1, 0); }
        }

        /// <summary>
        /// get string from cache indexed by absolute file index
        /// </summary>
        /// <param name="absoluteIndex">file row num. Starts from 0</param>
        /// <returns></returns>
        public string this[int absoluteIndex]
        {
            get {

                if ((absoluteIndex > fileRowNum) || (absoluteIndex < minAvail))
                    return null;

                int idx = (absoluteIndex - minAvail + index + 1) % size;
                return lines[idx];

            }
        }


        public jobCache(int cacheSize = 10000)
        {
            size = cacheSize;
            lines = new string[size];
        }
    }

    /// <summary>
    /// basic element - single chunk of job instance
    /// </summary>
    class jobPart
    {
        public int lineFrom;
        public int lineTo;
        public bool lineHit;
        public jobSaveStatsu saveStatus;
    }

    /// <summary>
    /// notNeedToSave-default, needToSave-not all chunks were in cache, Saving- saving all chunks; SavedSuccessfully - only for chunks
    /// </summary>
    enum jobSaveStatsu  {notNeedToSave, needToSave, Saving, SavedSuccessfully }; 

    /// <summary>
    /// element - collection of chunks
    /// </summary>
    class job
    {
        int jobID;
        int numberOfParts;
        int lastChunkIndex;
        int maxCapacity = 100; // na razie zakładam taki maksymalny rozmiar tablicy
        private jobPart[] list;
        string _fileName; //name of file to save job to;
        jobSaveStatsu saveStatus;
        jobCache globalCache;

        public int JobID { get => jobID; set => jobID = value; }

        /// <summary>
        /// Adds a new job and creates initial chunks
        /// </summary>
        /// <param name="ID">jobID</param>
        /// <param name="fromLine">statring line</param>
        /// <param name="toLine">ending line</param>
        /// <param name="hit">if job has to be saved</param>
        /// <param name="fileName">save to filename</param>
        public job(int ID, int fromLine, int toLine, string fileName, jobCache cache, bool hit = false)
        {
            list =  new jobPart[maxCapacity];
            JobID = ID;
            lastChunkIndex = 0;
            addChunk(fromLine, toLine, hit);
            _fileName = String.Format("{0}_[{1}].txt",fileName, JobID);
            globalCache = cache;
            // at this point there can be only one not saved chunk. I assume it will be in cache [we just processing this chunk]
            if (hit)
            {
                bool _saveStatus = false; //yes, I know. 
                _saveStatus = saveChunks();
                // logic of setting saveStatus moved to saveChunks;

            }
            else
            {
                saveStatus = jobSaveStatsu.notNeedToSave;
            }
        }


        protected string normalize(string line)
        {
            string txt = line;
            txt.Replace("&lt;", "<").Replace("&gt;", ">");
            return txt;
        }

        /// <summary>
        /// tries to save all chunks. all lines are appended.
        /// </summary>
        /// <returns></returns>
        private bool saveChunks()
        {
            StreamWriter sw = null;
            bool result = true;
            string line;
            try
            {
                sw = new StreamWriter(_fileName, true);
                foreach (jobPart chunk in GetChunk())
                {
                    if ((chunk.saveStatus != jobSaveStatsu.SavedSuccessfully)&&(globalCache.minAvail<chunk.lineFrom)) //REVIEW!!
                    {
                        for (int x = chunk.lineFrom; x< chunk.lineTo; x++)
                        {
                            line = globalCache[x];
                            if (line != null) //should not happen according to 2nd part of condition in if.
                            {
                                line = normalize(line);
                                sw.WriteLine(normalize(line));
                                Console.WriteLine("[I] Saved chunk for job {1}", JobID);
                            }
                            else
                            {
                                chunk.saveStatus = jobSaveStatsu.needToSave;
                                result = false;
                                Console.WriteLine("[E] Failed to save chunk for job {1}", JobID);
                            }
                        }
                        sw.WriteLine(String.Format("[{0}]:[{1}]--------------------------------------------------------", chunk.lineFrom, chunk.lineTo));
                        chunk.saveStatus = jobSaveStatsu.SavedSuccessfully;
                    }

                }

            }
            finally
            {
                sw?.Flush();
                sw?.Close();
                sw?.Dispose();
            }

            if (result)
            {
                saveStatus = jobSaveStatsu.Saving; //info for future chunks
            }
            else
            {
                saveStatus = jobSaveStatsu.needToSave; // for next run
            }

            return result;
        }

        public void addChunk(int fromLine, int toLine, bool isHit=false)
        {
            if (lastChunkIndex < maxCapacity)
            {
                list[lastChunkIndex].lineFrom = fromLine;
                list[lastChunkIndex].lineTo = toLine;
                list[lastChunkIndex].lineHit = isHit;
                lastChunkIndex++;

                if (isHit)
                    saveChunks();

                Console.WriteLine("[I] Job {0} inc length to {1}", JobID, list.Length);// ewentualnie lastChunkIndex
            }
            else
            {
                Console.WriteLine("[E] Job {0} has reached it max capacity", JobID);
              
            }
        }

        public IEnumerable<jobPart> GetChunk()
        {
            int index = 0;
            do
            {
                if ((index < 0)||(index > lastChunkIndex))
                    yield break;
                yield return list[index];
                index ++;
            } while (true);
        }

    }

    class LogParser
    {
        const string jobPrefix = @"[Job-";

        List<job> jobArray;
        jobCache cache;
        string FileName, SearchString;
        BoyerMoore search; 
        BoyerMoore jobS = new BoyerMoore(jobPrefix);

        public LogParser(string fileName, string searchString)
        {
            jobArray = new List<job>();
            cache = new jobCache();
            FileName = fileName;
            SearchString = searchString;
        }

        private int isInLine(BoyerMoore item, string line)
        {
            int result = -1;
            foreach (int index in item.BoyerMooreMatch(line))
            {
                result = index;
                break;
            }

            return result;
        }

        private int getJobID(string line)
        {
            int res = -1;
            int stop = -1;
            int start = line.IndexOf(jobPrefix);
            if (start != -1)
            {
                stop = line.IndexOf("]", start);
            }
            if (start != -1 && stop != -1)
            {
                string txt = line.Substring(start + 1, stop - start);
                int.TryParse(txt, out res);
            }
            return res;

        }

        // Run Forest, run....
        public void doSearch()
        {
            int currentLine = 0;
            int lastJobStart = 0;
            bool isJob;
            bool isItem;
            bool jobHit =false;
            job TMPJob;
            int TMPJobID;

            search = new BoyerMoore(SearchString);
            
            foreach (string line in File.ReadLines(FileName))
            {
                currentLine++;
                isJob = isInLine(jobS, line)!=-1;
                isItem = isInLine(search, line)!=-1;
                jobHit = isItem || jobHit;
                cache.addRow(line); //append to cache. It has to be priory any other operations.
                if (isJob) //save previous!
                {
                    if(lastJobStart != 0)
                    {
                        TMPJobID = getJobID(line);
                        TMPJob = jobArray.Find( x => x.JobID == TMPJobID ); //to check - probably it will search from beggining...
                        if (TMPJob != null)
                        { // add new job

                        }
                        else
                        { // add chunk

                        }
                    }

                    // celaning variables.
                    jobHit = false;
                    lastJobStart = currentLine;
                }

            }
        }

    }
}
