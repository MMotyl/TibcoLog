using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TibcoLog
{
    /// <summary>
    /// cache to handel last X lines;
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
    }

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
        public job(int ID, int fromLine, int toLine, bool hit=false)
        {
            list =  new jobPart[maxCapacity];
            jobID = ID;
            lastChunkIndex = 0;
            addChunk(fromLine, toLine, hit);
        }

        public void addChunk(int fromLine, int toLine, bool isHit=false)
        {
            if (lastChunkIndex < maxCapacity)
            {
                list[lastChunkIndex].lineFrom = fromLine;
                list[lastChunkIndex].lineTo = toLine;
                list[lastChunkIndex].lineHit = isHit;
                lastChunkIndex++;
                Console.WriteLine("Job {0} inc length to {1}", jobID, list.Length);// ewentualnie lastChunkIndex
            }
            else
            {
                Console.WriteLine("Job {0} has reached it max capacity", jobID);
              
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
        List<job> jobArray;
        public LogParser()
        {
            jobArray = new List<job>();
        }

    }
}
