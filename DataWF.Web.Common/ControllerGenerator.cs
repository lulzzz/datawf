﻿using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DataWF.Web.Common
{
    public class ControllerGenerator
    {
        [Obsolete()]
        public static void Generate(DBSchema schema)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(schema.Name), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeAttributes = TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout;
            foreach (var table in schema.Tables)
            {
                var itemType = table.GetType().GetGenericArguments().FirstOrDefault();
                var tableAttribute = DBTable.GetTableAttribute(itemType);
                var controllerType = typeof(DBController<>).MakeGenericType(itemType);
                var typeBuilder = moduleBuilder.DefineType(tableAttribute.ItemType.Name + "Controller", typeAttributes, controllerType);
                var apiControllerAttribute = new CustomAttributeBuilder(typeof(ApiControllerAttribute).GetConstructor(Type.EmptyTypes), new object[] { });
                typeBuilder.SetCustomAttribute(apiControllerAttribute);
                var routeAttribute = new CustomAttributeBuilder(typeof(RouteAttribute).GetConstructor(new[] { typeof(string) }), new object[] { "api/[controller]" });
                typeBuilder.SetCustomAttribute(routeAttribute);

                var constructor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            }
        }

        public static Assembly GenerateRoslyn(DBSchema schema)
        {
            var name = schema.Name.ToInitcap('_');
            var files = new List<SyntaxTree>();
            var references = new Dictionary<string, MetadataReference>() {
                {"netstandard", MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location) },
                {"System", MetadataReference.CreateFromFile(typeof(Object).Assembly.Location) },
                {"System.Runtime", MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=0.0.0.0").Location) },
                {"System.Collection", MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location) },
                {"Microsoft.AspNetCore.Mvc", MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location) },
                {"DataWF.Common", MetadataReference.CreateFromFile(typeof(Helper).Assembly.Location) },
                {"DataWF.Data", MetadataReference.CreateFromFile(typeof(DBTable).Assembly.Location) },
                {"DataWF.Web.Common", MetadataReference.CreateFromFile(typeof(DBController<>).Assembly.Location) }
            };
            var tree = (SyntaxTree)null;
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBController.cs");
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var sourceText = SourceText.From(file, Encoding.UTF8);
                tree = CSharpSyntaxTree.ParseText(sourceText);
            }
            var node = ((CompilationUnitSyntax)tree.GetRoot()).AddUsings(CreateUsingDirective("DataWF.Web.Common"));
            var namespaceNode = node.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var classNode = node.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            string baseClassName = classNode.Identifier.Text;


            foreach (var table in schema.Tables)
            {
                var itemType = table.GetType().GetGenericArguments().FirstOrDefault();
                var tableAttribute = DBTable.GetTableAttribute(itemType);
                var controllerType = typeof(DBController<>).MakeGenericType(itemType);
                string controllerClassName = $"{tableAttribute.ItemType.Name}Controller";

                var newImplementation = CSharpSyntaxTree.ParseText(
                  $@"namespace DataWF.Web.{name} 
{{
[Route(""api/[controller]"")]
[ApiController]
public class {controllerClassName} : {baseClassName}<{itemType.Name}>
{{
public {controllerClassName}() {{
// default ctor
}}
//TODO Methods from itemType
}}
}}
").GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();


                var newTree = CSharpSyntaxTree.Create(node
                    .ReplaceNode(namespaceNode, newImplementation)
                    .AddUsings(CreateUsingDirective(itemType.Namespace)).NormalizeWhitespace());
                
                var newSourceText = newTree.GetText();
                //var newFileName = $"{controllerClassName}.cs";
                //using (var newFile = new FileStream(newFileName, FileMode.Create, FileAccess.Write))
                //using (var writer = new StreamWriter(newFile))
                //    newSourceText.Write(writer);
                files.Add(newTree);
                if (!references.ContainsKey(itemType.Assembly.GetName().Name))
                    references[itemType.Assembly.GetName().Name] = MetadataReference.CreateFromFile(itemType.Assembly.Location);
            }
            CSharpCompilation compilation = CSharpCompilation.Create($"{name}.dll", files, references.Values,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
             diagnostic.IsWarningAsError ||
             diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    return Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
                }
            }
            return null;
        }

        //https://stackoverflow.com/a/36845547
        private static UsingDirectiveSyntax CreateUsingDirective(string usingName)
        {
            NameSyntax qualifiedName = null;

            foreach (var identifier in usingName.Split('.'))
            {
                var name = SyntaxFactory.IdentifierName(identifier);

                if (qualifiedName != null)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(qualifiedName, name);
                }
                else
                {
                    qualifiedName = name;
                }
            }

            return SyntaxFactory.UsingDirective(qualifiedName);
        }
    }
}