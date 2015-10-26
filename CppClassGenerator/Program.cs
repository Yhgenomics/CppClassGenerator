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
            if ( !CheckArgs( args ) )
            {
                return;
            }

            templatePath = args[0];

            GenerateEveryTemplate( templatePath );

        } // End of Main

        private static void GenerateEveryTemplate( string templatePath )
        {
            DirectoryInfo templetFolder = new DirectoryInfo(templatePath);
            allMsgNames = new List<string>();
            casesList = new List<string>();


            // Search in the templatePath
            foreach ( FileInfo NextFile in templetFolder.GetFiles( "*.json" ) )
            {
                userHdl = new List<string>();
                oneCaseLine = string.Empty;
                FilesManipulate( NextFile );

                // Read the templet file and put it in the List<MeatItem>
                List<MetaItem> classDefine = GetTemplate( templetFolder );

                // Control the indentation                
                indent = 0;

                BeginHeader( messageFile );

                CreateMessageHandlerFile( className );

                BeginNamespace();

                BeginClass();

                // The items that need to be use later in more than one phase
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
                        AppendInFile( messageFile , privateMember , indent );
                    }
                }
                EndPrivate( privateMembers );

                EndClass();
                EndNamespace();
                EndHeader();

                oneCaseLine = "    return " + className + "Handler( " + className + "( message ) );" + myNewLine;
                casesList.Add( oneCaseLine );
                oneCaseLine = "    break;" + myNewLine;
                casesList.Add( oneCaseLine );
                oneCaseLine = myNewLine;
                casesList.Add( oneCaseLine );
            } // End of search in the templatePath

            WriteBigMessagesHandlerFile();
        }

        private static void WriteBigMessagesHandlerFile()
        {

            if ( File.Exists( msgHdlFile ) )
            {
                File.Delete( msgHdlFile );
            }
            int MsgHdlIndent = 0;
            AppendInFile( msgHdlFile , "#ifndef MESSAGES_HANDLER_HPP_" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "#define MESSAGES_HANDLER_HPP_" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );

            AppendInFile( msgHdlFile , "#include \"stdlib.h\"" + myNewLine , MsgHdlIndent );
            foreach ( string msgName in allMsgNames )
            {
                AppendInFile( msgHdlFile , "#include \"" + msgName + "Handler.hpp\"" + myNewLine , MsgHdlIndent );
            }
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );

            AppendInFile( msgHdlFile , "namespace " + namespaceString + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "{" + myNewLine , MsgHdlIndent );
            MsgHdlIndent++;
            AppendInFile( msgHdlFile , "class MessagesHandler" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "{" + myNewLine , MsgHdlIndent );

            AppendInFile( msgHdlFile , "public:" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );
            MsgHdlIndent++;
            
            AppendInFile( msgHdlFile , "static int process( Message* message )" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "{" + myNewLine , MsgHdlIndent );
            MsgHdlIndent++;
            AppendInFile( msgHdlFile , "switch ( message->command() )" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "{" + myNewLine , MsgHdlIndent );
            MsgHdlIndent++;
            foreach ( string aCase in casesList )
            {
                AppendInFile( msgHdlFile , aCase , MsgHdlIndent );
            }
            AppendInFile( msgHdlFile , "default:" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "    return -1;" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "    break;" + myNewLine , MsgHdlIndent );
            MsgHdlIndent--;
            AppendInFile( msgHdlFile , "} // End of switch" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );

            MsgHdlIndent--;
            AppendInFile( msgHdlFile , "} // End of static int process" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );

            MsgHdlIndent--;

            AppendInFile( msgHdlFile , @"}; // End of class define of MessagesHandler" + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , myNewLine , MsgHdlIndent );

            MsgHdlIndent--;
            AppendInFile( msgHdlFile , @"} // End of namespace " + namespaceString + myNewLine , MsgHdlIndent );
            AppendInFile( msgHdlFile , "#endif" + myNewLine , MsgHdlIndent );

        }

        private static void CreateMessageHandlerFile( string className )
        {
            string handlerName = className+ "Handler";
            int handlerFileIndent = 0;
            AppendInFile( handlerFile , "#ifndef " + headerPrefix+"_HANDLER_HPP_" + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , "#define " + headerPrefix + "_HANDLER_HPP_" + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , myNewLine , handlerFileIndent );

            AppendInFile( handlerFile , "#include \"stdlib.h\"" + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , "#include \"" + className + ".hpp\"" + myNewLine , handlerFileIndent );

            AppendInFile( handlerFile , myNewLine , handlerFileIndent );

            AppendInFile( handlerFile , "namespace " + namespaceString + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , "{" + myNewLine , handlerFileIndent );
            handlerFileIndent++;

            AppendInFile( handlerFile , "static int " + handlerName + "( " + className + " msg )" + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , "{" + myNewLine , handlerFileIndent );
            handlerFileIndent++;

            AppendInFile( handlerFile , @"// UserDefineHandler Begin" + myNewLine , handlerFileIndent );
            if ( userHdl.Count == 0 )
            {
                AppendInFile( handlerFile , @"// Your Codes here!" + myNewLine , handlerFileIndent );
                AppendInFile( handlerFile , @"return 0;" + myNewLine , handlerFileIndent );
            }

            else
            {
                foreach ( string usercodes in userHdl )
                {
                    // No auto indent for user write codes
                    AppendInFile( handlerFile , usercodes + myNewLine , 0 );
                }
            }
            AppendInFile( handlerFile , @"// UserDefineHandler End " + myNewLine , handlerFileIndent );

            handlerFileIndent--;
            AppendInFile( handlerFile , "}" + myNewLine , handlerFileIndent );
            AppendInFile( handlerFile , myNewLine , handlerFileIndent );

            handlerFileIndent--;
            AppendInFile( handlerFile , @"} // End of namespace " + namespaceString + myNewLine , handlerFileIndent );

            AppendInFile( handlerFile , "#endif // !" + headerPrefix + "_HANDLER_HPP_" + myNewLine , handlerFileIndent );

        }

        private static void EndHeader()
        {
            AppendInFile( messageFile , "#endif // !" + headerPrefix + "_HPP_" + myNewLine , indent );
        }

        private static void EndNamespace()
        {
            indent--;
            AppendInFile( messageFile , myNewLine , indent );
            AppendInFile( messageFile , "} // End of namespace " + namespaceString + myNewLine , indent );//end of  namespace
        }

        private static void EndClass()
        {
            AppendInFile( messageFile , "};" + @" // End of class define of " + className + myNewLine , indent );//end of  class
        }

        private static void BeginClass()
        {
            AppendInFile( messageFile , "class " + className + " : public Message" + myNewLine , indent );
            AppendInFile( messageFile , "{" + myNewLine , indent );
        }

        private static void BeginNamespace()
        {
            AppendInFile( messageFile , "namespace " + namespaceString + myNewLine , indent );
            AppendInFile( messageFile , "{" + myNewLine , indent );
            indent++;
        }

        private static void EndPrivate( List<string> privateMembers )
        {
            indent--;
            if ( privateMembers.Count > 0 )
            {
                AppendInFile( messageFile , myNewLine , indent );
            }
        }

        private static void BeginPrivate()
        {
            AppendInFile( messageFile , "private:" + myNewLine , indent );
            AppendInFile( messageFile , myNewLine , indent );
            indent++;
        }

        private static void EndPublic()
        {
            indent--;
            AppendInFile( messageFile , myNewLine , indent );
        }

        private static void BeginPublic()
        {
            AppendInFile( messageFile , "public:" + myNewLine , indent );
            AppendInFile( messageFile , myNewLine , indent );
            indent++;
        }

        private static void AddConstructors( string messageValues , List<string> ConstructorContext , List<string> deserilizeParts )
        {
            // Constructors
            {
                // Constructor from params and set the private members to the default value defined in the json file
                AppendInFile( messageFile , @"// Serilize Constructor" + myNewLine , indent );
                context = className + "()" + myNewLine;
                AppendInFile( messageFile , context , indent );

                AppendInFile( messageFile , "    : Message( " + messageValues + " )" + myNewLine , indent );

                AppendInFile( messageFile , "{" + myNewLine , indent );

                indent++;

                foreach ( string item in ConstructorContext )
                {
                    AppendInFile( messageFile , item , indent );
                }

                indent--;
                AppendInFile( messageFile , "}" + myNewLine , indent );

                AppendInFile( messageFile , myNewLine , indent );

                // Constructor from a json string
                AppendInFile( messageFile , @"// Deserilize Constructor" + myNewLine , indent );
                context = className + "( "
                    + "Message* message"
                    + " )" + myNewLine;
                AppendInFile( messageFile , context , indent );

                AppendInFile( messageFile , "    : Message( *message )" + myNewLine , indent );

                AppendInFile( messageFile , "{" + myNewLine , indent );
                indent++;

                foreach ( string part in deserilizeParts )
                {
                    AppendInFile( messageFile , part , indent );
                }

                indent--;
                AppendInFile( messageFile , "}" + myNewLine , indent );

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
                if ( item.name.ToLower().Equals( "command" )
                  || item.name.ToLower().Equals( "status" )
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
                    if ( item.name.ToLower().Equals( "command" ) )
                    {
                        oneCaseLine = "case " + item.value + ":" + myNewLine;
                        casesList.Add( oneCaseLine );
                    }
                    continue;
                } // End of Skip the members in the base class, Message

                AddGetter( item );
                AddSetter( item );

                privateMembers.Add( item.type + " " + item.name.ToLower() + "_;" + myNewLine );
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
            AppendInFile( messageFile , @"// Setter of " + item.name.ToLower() + "_" + myNewLine , indent );
            AppendInFile( messageFile , "void " + item.name+ "( " + item.type + " value )" + myNewLine , indent );
            AppendInFile( messageFile , "{" + myNewLine , indent );
            indent++;
            AppendInFile( messageFile , item.name.ToLower() + "_ = value;" + myNewLine , indent );
            AppendInFile( messageFile , "raw_data_[ \"data\" ][ \"" + item.name + "\" ] = value;" + myNewLine , indent );
            indent--;
            AppendInFile( messageFile , "}" + myNewLine , indent );
            AppendInFile( messageFile , myNewLine , indent );
        }

        private static void AddGetter( MetaItem item )
        {
            AppendInFile( messageFile , @"// Getter of " + item.name.ToLower() + "_" + myNewLine , indent );
            AppendInFile( messageFile , item.type + " " + item.name+ "()" + myNewLine , indent );
            AppendInFile( messageFile , "{" + myNewLine , indent );
            indent++;
            AppendInFile( messageFile , "return " + item.name.ToLower() + "_;" + myNewLine , indent );
            indent--;
            AppendInFile( messageFile , "}" + myNewLine , indent );
            AppendInFile( messageFile , myNewLine , indent );
        }

        private static List<MetaItem> GetTemplate( DirectoryInfo templetFolder )
        {
            ReadJsonString( templetFolder + jsonFile );
            List<MetaItem> classDefine = JsonConvert.DeserializeObject<List<MetaItem>>( jsonString );
            return classDefine;
        }

        private static void FilesManipulate( FileInfo NextFile )
        {
            className = System.IO.Path.GetFileNameWithoutExtension( NextFile.FullName );
            jsonFile = className + ".json";
            messageFile = className + ".hpp";
            handlerFile = className + "Handler.hpp";
            allMsgNames.Add( className );


            // Message files need to be recreat
            if ( File.Exists( messageFile ) )
            {
                File.Delete( messageFile );
            }

            // Handler files need to keep the user's code
            if ( File.Exists( handlerFile ) )
            {
                //get the usr code part out and delete it
                System.IO.StreamReader handlerReader = new System.IO.StreamReader(handlerFile);
                string line = string.Empty;

                bool headpart = true;
                while ( ( line = handlerReader.ReadLine() ) != null )
                {
                    if ( headpart && !line.Contains( "// UserDefineHandler Begin" ) )
                        continue;
                    headpart = false;
                    if ( line.Contains( "// UserDefineHandler End" ) )
                    {
                        //userHdl.Add( line );
                        break;
                    }
                    if ( line.Contains( "// UserDefineHandler Begin" ) )
                        continue;
                    userHdl.Add( line );
                }

                handlerReader.Close();//关闭文件读取流

                File.Delete( handlerFile );
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
            for ( int i = 0 ; i < headerchar.Length - 4 ; i++ )
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
            headerPrefix = header;
            header += "_HPP_";

            context = header.ToUpper();
            AppendInFile( hppfile , "#ifndef " + context + myNewLine );
            AppendInFile( hppfile , "#define " + context + myNewLine );
            AppendInFile( hppfile , myNewLine );

            AppendInFile( hppfile , "#include \"Message.h\"" + myNewLine );
            AppendInFile( hppfile , "#include \"json.hpp\"" + myNewLine );
            AppendInFile( hppfile , "#include <stdlib.h>" + myNewLine );
            AppendInFile( hppfile , "#include <iostream>" + myNewLine );
            AppendInFile( hppfile , "#include <fstream>" + myNewLine );
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

        public static void AppendInFile( string filePath , string context , int indent = 0 )
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

        public static string jsonString        = string.Empty;
        public static string myNewLine         = "\r\n";
        public static string namespaceString   = @"Protocol";
        public static string jsonFile          = string.Empty;
        public static string messageFile       = string.Empty;
        public static string handlerFile       = string.Empty;
        public static string className         = string.Empty;
        public static string templatePath      = string.Empty;
        public static string context           = string.Empty;
        public static int    indent            = 0;
        public static List<string> userHdl     = null;
        public static List<string> allMsgNames = null;
        public static string msgHdlFile        = @"MessagesHandler.hpp";
        public static List<string> casesList   = null;
        public static string oneCaseLine       = null;
        public static string headerPrefix      = string.Empty;
    } // End of Class program
}
