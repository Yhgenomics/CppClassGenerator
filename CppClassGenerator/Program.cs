using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppClassGenerator
{
    class Program
    {
        static void Main( string[] args )
        {
            if(!CheckArgs(args))
            {
                return;
            }
            
            templatePath = args[0];

            GenerateEveryTemplate( templatePath );           
                
        } // End of Main

        private static void GenerateEveryTemplate( string templatePath )
        {
            DirectoryInfo templetFolder = new DirectoryInfo(templatePath);

            // Search in the templatePath
            foreach ( FileInfo NextFile in templetFolder.GetFiles( "*.json" ) )
            {
                CheckFileStatus( NextFile );

                // Read the templet file and put it in the List<MeatItem>
                List<MetaItem> classDefine = GetTemplate( templetFolder );

                // Control the indentation                
                indent = 0;              
                
                BeginHeader( hppfile );

                BeginNamespace();

                BeginClass();

                // Items need to be use later in more than one phase
                string messageValues;
                List<string> ConstructorContext, privateMembers, deserilizeParts;

                BeginPublic();
                {
                    DefineMemebers( classDefine , out messageValues , out ConstructorContext , out privateMembers , out deserilizeParts );
                    AddConstructors( messageValues , ConstructorContext , deserilizeParts );
                }
                EndPublic();

                BeginPrivate();
                {
                    foreach ( string privateMember in privateMembers )
                    {
                        AppendInFile( hppfile , privateMember , indent );
                    }
                }
                EndPrivate( privateMembers );

                EndClass();
                EndNamespace();
                EndHeader();

            } // End of search in the templatePath
        }

        private static void EndHeader()
        {
            AppendInFile( hppfile , "#endif " + myNewLine , indent );
        }

        private static void EndNamespace()
        {
            indent--;
            AppendInFile( hppfile , myNewLine , indent );
            AppendInFile( hppfile , "} //end of namespace " + namespaceStr + myNewLine , indent );//end of  namespace
        }

        private static void EndClass()
        {
            AppendInFile( hppfile , "};" + @" //end of class define of " + className + myNewLine , indent );//end of  class
        }

        private static void BeginClass()
        {
            AppendInFile( hppfile , "class " + className + " : public Message" + myNewLine , indent );
            AppendInFile( hppfile , "{" + myNewLine , indent );
        }

        private static void BeginNamespace()
        {
            AppendInFile( hppfile , "namespace " + namespaceStr + myNewLine , indent );
            AppendInFile( hppfile , "{" + myNewLine , indent );
            indent++;
        }

        private static void EndPrivate( List<string> privateMembers )
        {
            indent--;
            if ( privateMembers.Count > 0 )
            {
                AppendInFile( hppfile , myNewLine , indent );
            }
        }

        private static void BeginPrivate()
        {
            AppendInFile( hppfile , "private:" + myNewLine , indent );
            AppendInFile( hppfile , myNewLine , indent );
            indent++;
        }

        private static void EndPublic()
        {
            indent--;
            AppendInFile( hppfile , myNewLine , indent );
        }

        private static void BeginPublic()
        {
            AppendInFile( hppfile , "public:" + myNewLine , indent );
            AppendInFile( hppfile , myNewLine , indent );
            indent++;
        }

        private static void AddConstructors( string messageValues , List<string> ConstructorContext , List<string> deserilizeParts )
        {            
            // Constructors
            {
                // Constructor from params and set the private members to the default value defined in the json file
                AppendInFile( hppfile , @"// Serilize Constructor" + myNewLine , indent );
                context = className + "()" + myNewLine;
                AppendInFile( hppfile , context , indent );

                AppendInFile( hppfile , "    : Message( " + messageValues + " )" + myNewLine , indent );

                AppendInFile( hppfile , "{" + myNewLine , indent );

                indent++;

                foreach ( string item in ConstructorContext )
                {
                    AppendInFile( hppfile , item , indent );
                }

                indent--;
                AppendInFile( hppfile , "}" + myNewLine , indent );

                AppendInFile( hppfile , myNewLine , indent );

                // Constructor from a json string
                AppendInFile( hppfile , @"// Deserilize Constructor" + myNewLine , indent );
                context = className + "( "
                    + "string jsonBuffer"
                    + " )" + myNewLine;
                AppendInFile( hppfile , context , indent );

                AppendInFile( hppfile , "    : Message( jsonBuffer )" + myNewLine , indent );

                AppendInFile( hppfile , "{" + myNewLine , indent );
                indent++;

                foreach ( string part in deserilizeParts )
                {
                    AppendInFile( hppfile , part , indent );
                }

                indent--;
                AppendInFile( hppfile , "}" + myNewLine , indent );

            }// End of  Constructors
        }

        private static void DefineMemebers( List<MetaItem> classDefine , out string messageValues , out List<string> ConstructorContext , out List<string> privateMembers , out List<string> deserilizeParts )
        {
            // Memeber define               
            messageValues = string.Empty;
            ConstructorContext = new List<string>();
            List<string> dataContext = new List<string>();
            privateMembers = new List<string>();
            deserilizeParts = new List<string>();
            foreach ( var item in classDefine )
            {
                FixStringType( item );

                // Skip the members in the base class, Message
                if ( item.name.Equals( "command" )
                  || item.name.Equals( "status" )
                  || item.name.Equals( "from" )
                  || item.name.Equals( "to" )
                   )
                {
                    if ( messageValues.Equals( string.Empty ) )
                    {
                        messageValues += item.value + " ";
                    }
                    else
                    {
                        messageValues += ", " + item.value;
                    }
                    continue;
                } // End of Skip the members in the base class, Message

                AddGetter( item );
                AddSetter( item );

                privateMembers.Add( item.type + " " + item.name + "_;" + myNewLine );
                deserilizeParts.Add( "this->" + item.name.ToLower() + "_ = raw_data_[ \"data\" ][ \"" + item.name + "\" ].get<" + item.type + ">();" + myNewLine );
                ConstructorContext.Add( item.name + "( " + item.value + " );" + myNewLine );

            }// End of memeber define
            messageValues = "PROTOCOL_VERSION , " + messageValues;
        }

        private static void FixStringType( MetaItem item )
        {
            if ( item.type.ToLower().Equals( "string" ) )
            {
                item.value = "\"" + item.value + "\"";
            }
        }

        private static void AddSetter( MetaItem item )
        {
            AppendInFile( hppfile , @"// Setter of " + item.name.ToLower() + "_" + myNewLine , indent );
            AppendInFile( hppfile , "void " + item.name.ToLower() + "( " + item.type + " value )" + myNewLine , indent );
            AppendInFile( hppfile , "{" + myNewLine , indent );
            indent++;
            AppendInFile( hppfile , item.name.ToLower() + "_ = value;" + myNewLine , indent );
            AppendInFile( hppfile , "raw_data_[ \"data\" ][ \"" + item.name + "\" ] = value;" + myNewLine , indent );
            indent--;
            AppendInFile( hppfile , "}" + myNewLine , indent );
            AppendInFile( hppfile , myNewLine , indent );
        }

        private static void AddGetter( MetaItem item )
        {
            AppendInFile( hppfile , @"// Getter of " + item.name.ToLower() + "_" + myNewLine , indent );
            AppendInFile( hppfile , item.type + " " + item.name.ToLower() + "()" + myNewLine , indent );
            AppendInFile( hppfile , "{" + myNewLine , indent );
            indent++;
            AppendInFile( hppfile , "return " + item.name.ToLower() + "_;" + myNewLine , indent );
            indent--;
            AppendInFile( hppfile , "}" + myNewLine , indent );
            AppendInFile( hppfile , myNewLine , indent );
        }

        private static List<MetaItem> GetTemplate( DirectoryInfo templetFolder )
        {
            ReadJsonString( templetFolder + jsonfile );
            List<MetaItem> classDefine = JsonConvert.DeserializeObject<List<MetaItem>>( jsonString );
            return classDefine;
        }

        private static void CheckFileStatus( FileInfo NextFile )
        {
            className = System.IO.Path.GetFileNameWithoutExtension( NextFile.FullName );
            jsonfile = className + ".json";
            hppfile = className + ".hpp";

            if ( File.Exists( hppfile ) )
            {
                File.Delete( hppfile );
            }
        }

        private static bool CheckArgs( string[] args )
        {
            bool result = false;

            if ( args.Length == 1 && Directory.Exists( args[0] ) )
            {
                result = true;
            }

            else
            {
                Console.WriteLine( "Command format is :/n CppClassGenerator.exe [Message Templates path]" );
            }

            return result;
        }

        private static void BeginHeader( string hppfile )
        {
            string context;
            string header =string.Empty;
            char[] headerchar = hppfile.ToCharArray();

            //skip the tail of ".hpp" 
            for ( int i = 0 ;i<headerchar.Length - 4 ;i++ )
            {
                header += headerchar[i];

                if ( headerchar[i] >= 'a'
                && headerchar[i] <= 'z'
                && headerchar[i + 1] >= 'A'
                && headerchar[i + 1] <= 'Z' )
                {
                    header += '_';
                }
            }
            header += "_HPP";

            context = header.ToUpper();            
            AppendInFile( hppfile , "#ifndef " + context + myNewLine );
            AppendInFile( hppfile , "#define " + context + myNewLine );
            AppendInFile( hppfile , myNewLine );

            AppendInFile( hppfile , "#include \"Message.h\"" + myNewLine );
            AppendInFile( hppfile , "#include \"json.hpp\"" + myNewLine );
            AppendInFile( hppfile , "#include <stdlib.h>" + myNewLine );
            AppendInFile( hppfile , "#include <iostream>" + myNewLine );
            AppendInFile( hppfile , "#include <fstream>" + myNewLine );
            AppendInFile( hppfile , "#include <fstream>" + myNewLine );
            AppendInFile( hppfile , myNewLine );

            AppendInFile( hppfile , "using json = nlohmann::json" + myNewLine );
            AppendInFile( hppfile , myNewLine );
        }

        public class MetaItem
        {
            public string name = null;
            public string type = null;
            public string value = null; //default value
        }

        public static void ReadJsonString( string path )
        {
            jsonString = System.IO.File.ReadAllText( path , Encoding.UTF8 );
        }

        public static void AppendInFile( string filePath , string context, int indent = 0 )
        {
            FileStream fs = null;

            for ( int i = 0 ; i < indent ; i++ )
            {
                context = "    " + context;
            }
            Encoding encoder = Encoding.UTF8;
            byte[] bytes = encoder.GetBytes( context );
            try
            {
                fs = File.OpenWrite( filePath );
                fs.Position = fs.Length;               
                fs.Write( bytes , 0 , bytes.Length );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( "open file error {0}" , ex.ToString() );
            }
            finally
            {
                fs.Close();
            }         
        }

        public static string jsonString   = string.Empty;
        public static string myNewLine    = "\r\n";
        public static string namespaceStr = @"Message";
        public static string jsonfile     = string.Empty;
        public static string hppfile      = string.Empty;
        public static string className    = string.Empty;
        public static string templatePath = string.Empty;
        public static string context      = string.Empty;
        public static int    indent       = 0;
    } // End of Class program
}
