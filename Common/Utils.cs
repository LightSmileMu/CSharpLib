using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Utils
{
    public static float KahanSum(float[] values)
    {
        var sum = 0.0f;
        var c = 0.0f;       //A running compensation for lost low-order bits.
        for (int i = 0;i< values.Length;i++ )
        {
            var y = values[i] - c;    //So far, so good: c is zero.
            var t = sum + y;       //Alas, sum is big, y small, so low-order digits of y are lost.
            c = (t - sum) - y;  //(t - sum) recovers the high-order part of y; subtracting y recovers -(low part of y)
            sum = t; //Algebraically, c should always be zero. Beware eagerly optimising compilers!
            //Next time around, the lost low part will be added to y in a fresh attempt.
        }
        return sum;
    }
    
    private bool SerializeToFile<T>(string fileName,T data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            try
            {
                using (Stream fStream = new FileStream(fileName, FileMode.CreateNew,FileAccess.ReadWrite))
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    binFormat.Serialize(fStream, data);
                    fStream.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return false;
        }

        private bool DeserializeFromFile<T>(string fileName, out T data)
        {
            data = default(T);
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return false;
            }

            try
            {
                using (Stream fStream = new FileStream(fileName, FileMode.Open))
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    data = (T)binFormat.Deserialize(fStream);
                    fStream.Close();
                }
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }

            return false;
        }
}
