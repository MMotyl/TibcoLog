using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TibcoLog
{
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
