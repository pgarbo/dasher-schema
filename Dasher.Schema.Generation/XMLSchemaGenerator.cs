﻿using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Dasher.Schema.Generation
{
    public class XMLSchemaGenerator
    {
        public static XElement GenerateSchema(Type type)
        {
            var desc = GetDescription(type);
            var message = new XElement("Message", new XAttribute("name", type.Name), desc != null ? new XAttribute("description", desc) : null);
            GenerateSchema(type, message);
            return message;
        }

        private static string GetDescription(Type type)
        {
             var atrs = type.GetCustomAttributes(typeof(DasherSerialisableAttribute));
             var atr = (DasherSerialisableAttribute) atrs.FirstOrDefault();
             return atr?.Description;
        }

        private static void GenerateSchema(Type type, XElement parentField)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            if (ctors.Length != 1)
                throw new SchemaGenerationException("Type must have a single public constructor.", type);

            foreach (var ctorArg in type.GetConstructors().Single().GetParameters())
            {
                var ctorArgType = ctorArg.ParameterType;

                var listType = ctorArgType.GetInterfaces().SingleOrDefault(i => i.Name == "IReadOnlyCollection`1" && i.Namespace == "System.Collections.Generic");
                if (ctorArgType.IsEnum || listType != null || ctorArgType.Namespace == "System" || ctorArgType.IsValueType || ctorArgType.IsEnum)
                {
                    var fieldElem = new XElement("Field",
                                new XAttribute("name", ctorArg.Name),
                                new XAttribute("type", ctorArg.ParameterType));
                    if (ctorArg.HasDefaultValue)
                    {
                        fieldElem.Add(new XAttribute("default", ctorArg.DefaultValue == null ? "null" : ctorArg.DefaultValue));
                    }
                    parentField.Add(fieldElem);
                }
                else
                {
                    var fieldElem = new XElement("Field",
                                new XAttribute("name", ctorArg.Name),
                                new XAttribute("type", ctorArg.ParameterType));
                    if (ctorArg.HasDefaultValue)
                    {
                        fieldElem.Add(new XAttribute("default", ctorArg.DefaultValue == null ? "null" : ctorArg.DefaultValue));
                    }
                    GenerateSchema(ctorArg.ParameterType, fieldElem);
                    parentField.Add(fieldElem);
                }
            }
        }
    }
}