using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyBot
{
    class DNA
    {
        public int targetlength;
        public float mutationRate;
        public string targetText;
        public char[] genesArray;
        public float fitness { get; set; }

        public DNA(float mutation, string target)
        {
            targetText = target;
            mutationRate = mutation;
            targetlength = target.Length;
            genesArray = new char[targetlength];
        }

        /* Clone dna */
        public DNA(DNA dna)
        {
            fitness = dna.fitness;
            genesArray = dna.genesArray;
            targetText = dna.targetText;
            mutationRate = dna.mutationRate;
            targetlength = dna.targetlength;
        }

        /* Create Child */
        public DNA(DNA p1, DNA p2, System.Random rand)
        {
            targetText = p1.targetText;
            mutationRate = p1.mutationRate;
            targetlength = p1.targetlength;
            genesArray = new char[targetlength];
            for (int i = 0; i < targetlength; ++i)
            {
                if (rand.Next(2) == 1)
                {
                    genesArray[i] = p1.genesArray[i];
                }
                else
                {
                    genesArray[i] = p2.genesArray[i];
                }
            }
            MutateGenes(rand);
            CalculateFitness();
        }

        public void InitGenes(System.Random rand)
        {
            for (int i = 0; i < targetlength; ++i)
            {
                int character = rand.Next(32, 127);
                genesArray[i] = System.Convert.ToChar(character);
            }
        }

        public void MutateGenes(System.Random rand)
        {
            for (int i = 0; i < targetlength; ++i)
            {
                if (rand.NextDouble() < mutationRate)
                {
                    int character = rand.Next(32, 127);
                    genesArray[i] = System.Convert.ToChar(character);
                }
            }
        }

        public void PrintGenes()
        {
            System.Console.WriteLine(genesArray);
        }

        public void CalculateFitness()
        {
            float score = 0.0F;
            for (int i = 0; i < targetlength; ++i)
            {
                if (genesArray[i] == targetText[i])
                {
                    score += 1;
                }
            }
            fitness = score / targetlength;
        }
    }
}
