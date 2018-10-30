using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Web.Services.Protocols;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace CallJavaWebServiceDemo
{
    public static class WebServiceHelper
    {
        /// <summary>
        ///     动态调用WebService
        /// </summary>
        /// <param name="url">WebService地址</param>
        /// <param name="methodname">方法名(模块名)</param>
        /// <param name="args">参数列表,无参数为null</param>
        /// <returns>object</returns>
        public static object InvokeWebService(string url, string methodname, object[] args)
        {
            return InvokeWebService(url, null, methodname, args);
        }

        /// <summary>
        ///     动态调用WebService
        /// </summary>
        /// <param name="url">WebService地址</param>
        /// <param name="classname">类名</param>
        /// <param name="methodname">方法名(模块名)</param>
        /// <param name="args">参数列表</param>
        /// <returns>object</returns>
        public static object InvokeWebService(string url, string classname, string methodname, object[] args)
        {
            string baseWsdlUrl = url;
            if (string.IsNullOrEmpty(classname))
            {
                classname = GetClassName(baseWsdlUrl);
            }
            
            var wc = new WebClient();

            //add by fans 2017.8.21 判读后缀有无?WSDL，没有则拼接
            Stream stream;
            if (baseWsdlUrl.Substring(baseWsdlUrl.Length - 5, 5).ToUpper() == "?WSDL")
            {
                stream = wc.OpenRead(baseWsdlUrl); //获取服务描述语言(WSDL) 
            }
            else
            {
                baseWsdlUrl = baseWsdlUrl + "?WSDL";
                stream = wc.OpenRead(baseWsdlUrl); //获取服务描述语言(WSDL) 
            }

            if (stream == null)
            {
                return null;
            }

            var sd = ServiceDescription.Read(stream); //通过直接从 Stream实例加载 XML 来初始化ServiceDescription类的实例。  
            var serviceDescriptionImporter = new ServiceDescriptionImporter
            {
                ProtocolName = "Soap",
                Style = ServiceDescriptionImportStyle.Client,
                CodeGenerationOptions =CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync
            };

            serviceDescriptionImporter.AddServiceDescription(sd, "", "");
            var cn = new CodeNamespace(); 
            //生成客户端代理类代码  
            var ccu = new CodeCompileUnit();
            ccu.Namespaces.Add(cn);
            CheckForImports(baseWsdlUrl, serviceDescriptionImporter);
            serviceDescriptionImporter.Import(cn, ccu);
            
            CSharpCodeProvider csc = new CSharpCodeProvider();
            var icc = csc.CreateCompiler(); //取得C#程式码编译器的执行个体  

            //设定编译器的参数  
            var cplist = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            }; 
            //创建编译器的参数实例  
            cplist.ReferencedAssemblies.Add("System.dll");
            cplist.ReferencedAssemblies.Add("System.XML.dll");
            cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
            cplist.ReferencedAssemblies.Add("System.Data.dll");
            //编译代理类  
            var cr = icc.CompileAssemblyFromDom(cplist, ccu);
            if (cr.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                foreach (CompilerError ce in cr.Errors)
                {
                    sb.Append(ce);
                    sb.Append(Environment.NewLine);
                }
                throw new Exception(sb.ToString());
            }

            //生成代理实例,并调用方法  
            var assembly = cr.CompiledAssembly;
            var types = assembly.GetTypes();
            var objTypeName = "";
            foreach (var type in types)
            {
                if (type.BaseType == typeof(SoapHttpClientProtocol))
                {
                    objTypeName = type.Name;
                    break;
                }
            }

            var t = assembly.GetType(objTypeName, true, true);
            var obj = Activator.CreateInstance(t);
            var mi = t.GetMethod(methodname);
            //MethodInfo 的实例可以通过调用GetMethods或者Type对象或派生自Type的对象的GetMethod方法来获取，还可以通过调用表示泛型方法定义的 MethodInfo 的MakeGenericMethod方法来获取。  
            if (mi != null)
            {
                return mi.Invoke(obj, args);
            }

            return null;
        }

        private static void CheckForImports(string baseWsdlUrl, ServiceDescriptionImporter importer)
        {
            var dcp = new DiscoveryClientProtocol();
            dcp.DiscoverAny(baseWsdlUrl);
            dcp.ResolveAll();
            foreach (var osd in dcp.Documents.Values)
            {
                if (osd is ServiceDescription)
                {
                    importer.AddServiceDescription((ServiceDescription) osd, null, null);
                }
                ;
                if (osd is XmlSchema)
                {
                    importer.Schemas.Add((XmlSchema) osd);
                }
            }
        }

        private static string GetClassName(string url)
        {
            //假如URL为"http://localhost/InvokeService/Service1.asmx"  
            //最终的返回值为 Service1  
            var parts = url.Split('/');
            var pps = parts[parts.Length - 1].Split('.');
            if (pps[0].Contains("?wsdl"))
            {
                return pps[0].Replace("?wsdl", string.Empty);
            }
            if (pps[0].Contains("?WSDL"))
            {
                return pps[0].Replace("?WSDL", string.Empty);
            }
            return pps[0];
        }

        public static bool IsWebServiceAvaiable(string url)
        {
            try
            {
                var myHttpWebRequest = (HttpWebRequest) WebRequest.Create(url);
                myHttpWebRequest.Timeout = 30000;
                using (var myHttpWebResponse = (HttpWebResponse) myHttpWebRequest.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                }
            }
            catch (Exception e)
            {
            }
            return false;
        }
    }
}