#include <iostream>
#include <fstream>
#include <string>
#include <vector>

#include "Source/src/glsl/glsl_optimizer.h"

static std::vector<char> ReadBinaryFile(const char* fileName)
{
	std::ifstream ifs(fileName, std::ios::binary | std::ios::ate);
	std::ifstream::pos_type pos = ifs.tellg();

	std::vector<char> result(pos);

	ifs.seekg(0, std::ios::beg);
	ifs.read(&result[0], pos);

	return result;
}

int main(int argc, const char* argv[])
{
	auto targetVersion = (glslopt_target)std::stoi(argv[2]);
	auto shaderType = (glslopt_shader_type)std::stoi(argv[3]);

	auto shaderBytes = ReadBinaryFile(argv[1]);

	auto ctx = glslopt_initialize(targetVersion);

	auto shader = glslopt_optimize(ctx, shaderType, shaderBytes.data(), 0);
	if (glslopt_get_status(shader)) {
		auto newSource = glslopt_get_output(shader);

		std::ofstream out(argv[4], std::ios::out);
		out << newSource;
		out.close();
	}
	else {
		auto errorLog = glslopt_get_log(shader);

		std::ofstream out(argv[5], std::ios::out);
		out << errorLog;
		out.close();
	}
	glslopt_shader_delete(shader);

	glslopt_cleanup(ctx);

	return 0;
}