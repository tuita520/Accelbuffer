﻿using accelc.Properties;
using System;
using System.IO;
using System.Linq;

namespace accelc.Compiler
{
    public sealed class Generator
    {
        private readonly Declaration[] m_Declarations;
        private readonly StreamWriter m_Writer;
        private int m_Depth;

        public Generator(Declaration[] declarations, string outputPath)
        {
            m_Declarations = declarations;
            m_Writer = new StreamWriter(outputPath, false);
            m_Depth = 0;
        }

        public void Generate()
        {
            GenerateCommment();

            for (int i = 0; i < m_Declarations.Length; i++)
            {
                switch (m_Declarations[i])
                {
                    case UsingDeclaration ud:
                        GenerateUsing(ud);
                        break;

                    case NamespaceDeclaration nsd:
                        i++;
                        GenerateNamespace(nsd, ref i);
                        break;

                    case TypeDeclaration td:
                        GenerateType(td);
                        break;
                }
            }

            m_Writer.Dispose();
        }

        private void GenerateCommment()
        {
            WriteLine("//------------------------------------------------------------------------------");
            WriteLine("// <auto-generated>");
            WriteLine("//     此代码由工具生成。");
            WriteLine("//     编译器版本:" + Resources.Version);
            WriteLine("//");
            WriteLine("//     对此文件的更改可能会导致不正确的行为，并且如果");
            WriteLine("//     重新生成代码，这些更改将会丢失。");
            WriteLine("// </auto-generated>");
            WriteLine("//------------------------------------------------------------------------------");
            WriteLine();
        }

        private void GenerateNamespace(NamespaceDeclaration declaration, ref int i)
        {
            WriteLine();
            WriteLine(Resources.Namespace + declaration.Name);

            using (new Block(this))
            {
                for (; i < m_Declarations.Length; i++)
                {
                    TypeDeclaration type = m_Declarations[i] as TypeDeclaration;
                    GenerateType(type);
                }
            }
        }

        private void GenerateUsing(UsingDeclaration declaration)
        {
            WriteLine(Resources.Using + declaration.Name + Resources.Semicolon);
        }

        private void GenerateType(TypeDeclaration declaration)
        {
            int index = 1;

            WriteLine();

            if (declaration.Doc != null)
            {
                WriteDoc(declaration.Doc);
            }

            if (declaration.IsCompact)
            {
                WriteLine(Resources.CompactLayout);
            }

            WriteLine(Resources.Serializable);

            if (declaration.InitMemory != null)
            {
                WriteLine(string.Format(Resources.InitialMemorySize, declaration.InitMemory.Value));
            }

            if (!declaration.IsRuntime)
            {
                WriteLine(string.Format(Resources.SerializeBy, declaration.Name));
            }

            if (declaration.IsInternal)
            {
                Write(Resources.Internal);
            }
            else
            {
                Write(Resources.Public);
            }

            if (declaration.IsFinal && declaration.IsRef)
            {
                Write(Resources.Sealed, false);
            }

            if (declaration.IsRef)
            {
                Write(Resources.Class, false);
            }
            else
            {
                Write(Resources.Struct, false);
            }

            WriteLine(declaration.Name, false);

            using (new Block(this))
            {
                foreach (Declaration d in declaration.Declarations)
                {
                    switch (d)
                    {
                        case FieldDeclaration field:
                            WriteField(field, ref index);
                            break;

                        case TypeDeclaration type:
                            GenerateType(type);
                            break;
                    }
                }

                if (declaration.Before != null)
                {
                    WriteMessage(declaration.Before, false);
                }

                if (declaration.After != null)
                {
                    WriteMessage(declaration.After, true);
                }

                if (declaration.IsRef)
                {
                    WriteLine();

                    Write(Resources.Public);

                    WriteLine(string.Format(Resources.DefaultCtor, declaration.Name), false);
                }
            }

            if (!declaration.IsRuntime)
            {
                GenerateSerializer(declaration);
            }
        }

        private void GenerateSerializer(TypeDeclaration declaration)
        {
            WriteLine();
            WriteLine(string.Format(Resources.SerializerType, declaration.Name, declaration.Name));

            using (new Block(this))
            {
                WriteLine(string.Format(Resources.SerializeMethod, declaration.Name));

                using (new Block(this))
                {
                    int index = 1;

                    if (declaration.Before != null)
                    {
                        WriteLine(Resources.BeforeMethod);
                    }

                    foreach (Declaration d in declaration.Declarations)
                    {
                        if (d is FieldDeclaration field)
                        {
                            Block block = null;

                            if (field.IsCheckref)
                            {
                                WriteLine(string.Format(Resources.WriteValueCheckRef, field.Name));
                                block = new Block(this);
                            }

                            switch (field.Type)
                            {
                                case "sbyte":
                                case "byte":
                                case "short":
                                case "ushort":
                                case "int":
                                case "uint":
                                case "long":
                                case "ulong":

                                    if (declaration.IsCompact)
                                    {
                                        WriteLine(string.Format(Resources.WriteValueIntCompact, field.Name, field.IsFixed ? Resources.NumberFormat_Fixed : Resources.NumberFormat_Variant));
                                    }
                                    else
                                    {
                                        WriteLine(string.Format(Resources.WriteValueInt, index++, field.Name, field.IsFixed ? Resources.NumberFormat_Fixed : Resources.NumberFormat_Variant));
                                    }

                                    break;

                                case "float":
                                case "double":
                                case "decimal":
                                case "bool":
                                    if (declaration.IsCompact)
                                    {
                                        WriteLine(string.Format(Resources.WriteValueFloatBoolCompact, field.Name));
                                    }
                                    else
                                    {
                                        WriteLine(string.Format(Resources.WriteValueFloatBool, index++, field.Name));
                                    }

                                    break;

                                case "char":
                                case "string":

                                    if (declaration.IsCompact)
                                    {
                                        if (field.IsUnicode)
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringUnicodeCompact, field.Name));
                                        }
                                        else if (field.IsASCII)
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringASCIICompact, field.Name));
                                        }
                                        else
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringUTF8Compact, field.Name));
                                        }

                                    }
                                    else
                                    {
                                        if (field.IsUnicode)
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringUnicode, index++, field.Name));
                                        }
                                        else if (field.IsASCII)
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringASCII, index++, field.Name));
                                        }
                                        else
                                        {
                                            WriteLine(string.Format(Resources.WriteValueStringUTF8, index++, field.Name));
                                        }
                                    }

                                    break;

                                default:
                                    if (declaration.IsCompact)
                                    {
                                        WriteLine(string.Format(Resources.WriteValueComplexCompact, field.Type, field.Name));
                                    }
                                    else
                                    {
                                        WriteLine(string.Format(Resources.WriteValueComplex, field.Type, index++, field.Name));
                                    }
                                    break;
                            }

                            block?.Dispose();
                        }
                    }
                }

                WriteLine(string.Format(Resources.DeserializeMethod, declaration.Name));

                using (new Block(this))
                {
                    WriteLine(string.Format(Resources.New, declaration.Name, declaration.Name));
                    WriteLine(Resources.DefIndex);

                    if (declaration.IsCompact)
                    {
                        WriteLine(Resources.WhileCompact);
                    }
                    else
                    {
                        WriteLine(Resources.While);
                    }

                    int index = declaration.IsCompact ? 0 : 1;

                    using (new Block(this))
                    {
                        WriteLine(Resources.Switch);

                        using (new Block(this))
                        {
                            foreach (Declaration d in declaration.Declarations)
                            {
                                if (d is FieldDeclaration field)
                                {
                                    WriteLine(string.Format(Resources.Case, index++));
                                    string type = GetTypeName(field.Type);
                                    m_Depth++;

                                    if (declaration.IsCompact)
                                    {
                                        if (type == string.Empty)
                                        {
                                            WriteLine(string.Format(Resources.NextAsComplexCompact, field.Name, field.Type));
                                        }
                                        else
                                        {
                                            switch (field.Type)
                                            {
                                                case "sbyte":
                                                case "byte":
                                                case "short":
                                                case "ushort":
                                                case "int":
                                                case "uint":
                                                case "long":
                                                case "ulong":
                                                    WriteLine(string.Format(Resources.NextAsIntCompact, field.Name, type, field.IsFixed ? Resources.NumberFormat_Fixed : Resources.NumberFormat_Variant));
                                                    break;

                                                case "float":
                                                case "double":
                                                case "decimal":
                                                case "bool":
                                                    WriteLine(string.Format(Resources.NextAsCompact, field.Name, type));
                                                    break;

                                                case "char":
                                                case "string":

                                                    if (field.IsUnicode)
                                                    {
                                                        WriteLine(string.Format(Resources.NextAsCharStringUnicodeCompact, field.Name, type));
                                                    }
                                                    else if (field.IsASCII)
                                                    {
                                                        WriteLine(string.Format(Resources.NextAsCharStringASCIICompact, field.Name, type));
                                                    }
                                                    else
                                                    {
                                                        WriteLine(string.Format(Resources.NextAsCharStringUTF8Compact, field.Name, type));
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (type == string.Empty)
                                        {
                                            WriteLine(string.Format(Resources.NextAsComplex, field.Name, field.Type));
                                        }
                                        else
                                        {
                                            WriteLine(string.Format(Resources.NextAs, field.Name, type));
                                        }
                                    }

                                    WriteLine(Resources.Break);
                                    m_Depth--;
                                }
                            }

                            WriteLine(Resources.Defeault);
                            m_Depth++;

                            if (declaration.IsCompact)
                            {
                                if (declaration.After != null)
                                {
                                    WriteLine(Resources.AfterMethod);
                                }
                                WriteLine(Resources.ReturnResult);
                            }
                            else
                            {
                                WriteLine(Resources.SkipNext);
                                WriteLine(Resources.Break);
                            }

                            m_Depth--;
                        }

                        if (declaration.IsCompact)
                        {
                            WriteLine(Resources.IncreaseIndex);
                        }
                    }

                    if (declaration.After != null)
                    {
                        WriteLine(Resources.AfterMethod);
                    }
                    WriteLine(Resources.ReturnResult);
                }
            }
        }

        private string GetTypeName(string type)
        {
            switch (type)
            {
                case "sbyte": return "Int8";
                case "byte": return "UInt8";
                case "short": return "Int16";
                case "ushort": return "UInt16";
                case "int": return "Int32";
                case "uint": return "UInt32";
                case "long": return "Int64";
                case "ulong": return "UInt64";
                case "float": return "Float32";
                case "double": return "Float64";
                case "decimal": return "Float128";
                case "bool": return "Boolean";
                case "char": return "Char";
                case "string": return "String";
                default: return string.Empty;
            }
        }

        private void WriteDoc(string doc)
        {
            string[] lines = doc.Split('\n');

            WriteLine("/// <summary>");

            foreach (string line in lines)
            {
                WriteLine($"/// {line.Trim()}");
            }

            WriteLine("/// </summary>");
        }

        private void WriteField(FieldDeclaration field, ref int index)
        {
            if (field.Doc != null)
            {
                WriteDoc(field.Doc);
            }

            if (field.IsFixed)
            {
                WriteLine(Resources.Fixed);
            }

            if (field.IsCheckref)
            {
                WriteLine(Resources.CheckRef);
            }

            WriteLine(string.Format(Resources.FieldIndex, index++));

            if (field.IsUnicode)
            {
                WriteLine(Resources.EncodingUnicode);
            }
            else if (field.IsASCII)
            {
                WriteLine(Resources.EncodingASCII);
            }
            else if (field.IsUTF8)
            {
                WriteLine(Resources.EncodingUTF8);
            }

            if (string.IsNullOrEmpty(field.Assignment))
            {
                WriteLine(string.Format(Resources.Field, field.Type, field.Name));
            }
            else
            {
                WriteLine(string.Format(Resources.FieldWithAssignment, field.Type, field.Name, field.Assignment));
            }
        }

        private void WriteMessage(MessageDeclaration message, bool isAfter)
        {
            WriteLine();

            if (isAfter)
            {
                WriteLine(Resources.AfterAttr);
                WriteLine(Resources.After);
            }
            else
            {
                WriteLine(Resources.BeforeAttr);
                WriteLine(Resources.Before);
            }

            using (new Block(this))
            {
                string[] lines = message.Code.Split('\n');

                foreach (string line in lines)
                {
                    WriteLine(line.Trim());
                }
            }
        }

        private void Write(string text, bool writeIndent = true)
        {
            for (int i = 0; i < m_Depth && writeIndent; i++)
            {
                m_Writer.Write("    ");
            }

            m_Writer.Write(text);
        }

        private void WriteLine(string text, bool writeIndent = true)
        {
            for (int i = 0; i < m_Depth && writeIndent; i++)
            {
                m_Writer.Write("    ");
            }

            m_Writer.WriteLine(text);
        }

        private void WriteLine()
        {
            m_Writer.WriteLine();
        }

        private sealed class Block : IDisposable
        {
            private readonly Generator m_Generator;

            public Block(Generator generator)
            {
                m_Generator = generator;
                m_Generator.WriteLine("{");
                m_Generator.m_Depth++;
            }

            public void Dispose()
            {
                m_Generator.m_Depth--;
                m_Generator.WriteLine("}");
            }
        }
    }
}
