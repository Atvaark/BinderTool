using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BinderTool.Core.Sl2
{
    public class Sl2File
    {
        private const string Sl2Signature = "BND4";
        private readonly List<Sl2UserData> _userData;

        public Sl2File()
        {
            _userData = new List<Sl2UserData>();
        }

        public List<Sl2UserData> UserData
        {
            get { return _userData; }
        }

        public static Sl2File ReadSl2File(Stream inputStream)
        {
            Sl2File sl2File = new Sl2File();
            sl2File.Read(inputStream);
            return sl2File;
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);
            BinaryReader unicodeReader = new BinaryReader(inputStream, Encoding.Unicode, true);
            string signature = reader.ReadString(4);
            if (signature != Sl2Signature)
                throw new Exception("Unknown signature");
            reader.Skip(8);
            int userDataCount = reader.ReadInt32();
            reader.Skip(8);
            string version = reader.ReadString(8);
            int directoryEntrySize = reader.ReadInt32();
            reader.Skip(4);
            int dataOffset = reader.ReadInt32();
            reader.Skip(20);

            // Directory section
            for (int i = 0; i < userDataCount; i++)
            {
                reader.Skip(8);
                int userDataSize = reader.ReadInt32();
                reader.Skip(4);

                int userDataOffset = reader.ReadInt32();
                int userDataNameOffset = reader.ReadInt32();
                reader.Skip(8);


                long position = reader.GetPosition();
                string fileName = "";
                if (userDataNameOffset > 0)
                {
                    reader.Seek(userDataNameOffset);
                    fileName = unicodeReader.ReadNullTerminatedString();
                }
                reader.Seek(userDataOffset);
                _userData.Add(Sl2UserData.ReadSl2UserData(inputStream, userDataSize, fileName));
                reader.Seek(position);
            }
        }

        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.ASCII, true);
            // TODO: Implement Sl2File.Write
            throw new NotImplementedException();
        }
    }
}
