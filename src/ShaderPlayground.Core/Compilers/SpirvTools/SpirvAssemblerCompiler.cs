using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.SpirvTools
{
    internal sealed class SpirvAssemblerCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvAssembler;
        public string DisplayName { get; } = "spirv-as";
        public string Url { get; } = "https://github.com/KhronosGroup/SPIRV-Tools#assembler-binary-parser-and-disassembler";
        public string Description { get; } = "Reads the assembly language text, and emits the binary form.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirvAssembly };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("spirv-tools"),
            new ShaderCompilerParameter("TargetEnv", "Target environment", ShaderCompilerParameterType.ComboBox, TargetEnvOptions, defaultValue: "spv1.0"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        private static readonly string[] TargetEnvOptions =
        {
            "vulkan1.0",
            "vulkan1.1",
            "vulkan1.2",
            "vulkan1.3",
            "spv1.0",
            "spv1.1",
            "spv1.2",
            "spv1.3",
            "spv1.4",
            "spv1.5",
            "spv1.6",
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-as.exe"),
                    $"-o \"{outputPath}\" --preserve-numeric-ids --target-env {arguments.GetString("TargetEnv")} \"{tempFile.FilePath}\"",
                    out var _,
                    out var stdError);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                var hasCompilationError = !string.IsNullOrEmpty(stdError);

                string textOutput = null;
                if (!hasCompilationError)
                {
                    var textOutputPath = $"{tempFile.FilePath}.txt";

                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-dis.exe"),
                        $"-o \"{textOutputPath}\" \"{outputPath}\"",
                        out var _,
                        out var _);

                    textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                    FileHelper.DeleteIfExists(textOutputPath);
                }

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, binaryOutput) : null,
                    hasCompilationError ? (int?) 1 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput),
                    new ShaderCompilerOutput("Build errors", null, stdError));
            }
        }
    }
}
