using System.IO;

namespace Utilities.IO
{
    class File
    {
        public void writeToFile(MemoryStream ms, string filename)
        {
            ms.Position = 0;
            using (FileStream file = new FileStream(filename, FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[ms.Length];
                ms.Read(bytes, 0, (int)ms.Length);
                file.Write(bytes, 0, bytes.Length);
                //ms.Close();
            }
        }

        public void readFromFile(MemoryStream ms, string filename)
        {
            ms.Position = 0;
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                ms.Write(bytes, 0, (int)file.Length);
            }

        }
    }
}
