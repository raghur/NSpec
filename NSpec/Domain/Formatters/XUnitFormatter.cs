﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace NSpec.Domain.Formatters
{
    [Serializable]
    public class XUnitFormatter : IFormatter
    {
        string file;

        public void Write(ContextCollection contexts)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            XmlTextWriter xml = new XmlTextWriter(sw);

            xml.WriteStartElement("testsuites");
            xml.WriteAttributeString("tests", contexts.Examples().Count().ToString());
            xml.WriteAttributeString("errors", "0");
            xml.WriteAttributeString("failures", contexts.Failures().Count().ToString());
            xml.WriteAttributeString("skip", contexts.Pendings().Count().ToString());

            contexts.Do(c => this.BuildContext(xml, c));
            xml.WriteEndElement();
            var results = sb.ToString();
            bool didWriteToFile = false;
            if (Options.ContainsKey("file"))
            {
                var filePath = Path.Combine(Environment.CurrentDirectory, Options["file"]);
                using (StreamWriter ostream = new StreamWriter(filePath, false))
                {
                    ostream.WriteLine(results);
                    Console.WriteLine("Test results published to: {0}".With(filePath));
                }
                didWriteToFile = true;
            }
            if (didWriteToFile && Options.ContainsKey("console"))
                Console.WriteLine(results);

            if (!didWriteToFile)
                Console.WriteLine(results);
        }

        public IDictionary<string, string> Options { get; set; }

        void BuildContext(XmlTextWriter xml, Context context)
        {
            if (context.Level == 1)
            {
                xml.WriteStartElement("testsuite");
                xml.WriteAttributeString("tests", context.AllExamples().Count().ToString());
                xml.WriteAttributeString("name", context.Name);
                xml.WriteAttributeString("errors", "0");
                xml.WriteAttributeString("failures", context.Failures().Count().ToString());
            }

            context.Examples.Do(e => this.BuildSpec(xml, e));
            context.Contexts.Do(c => this.BuildContext(xml, c));

            if (context.Level == 1)
            {
                xml.WriteEndElement();
            }
        }

        void BuildSpec(XmlTextWriter xml, ExampleBase example)
        {
            xml.WriteStartElement("testcase");

            string testName = example.Spec;
            StringBuilder className = new StringBuilder();
            ComposePartialName(example.Context, className);

            xml.WriteAttributeString("classname", className.ToString());
            xml.WriteAttributeString("name", testName);

            if (example.Exception != null)
            {
                xml.WriteStartElement("failure");
                xml.WriteAttributeString("type", example.Exception.GetType().Name);
                xml.WriteAttributeString("message", example.Exception.Message);
                xml.WriteString(example.Exception.ToString());
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        private void ComposePartialName(Context context, StringBuilder contextName)
        {
            if (context.Level <= 1) { return; }

            ComposePartialName(context.Parent, contextName);
            if (contextName.Length > 0) { contextName.Append(", "); }

            contextName.Append(context.Name);
        }
    }
}