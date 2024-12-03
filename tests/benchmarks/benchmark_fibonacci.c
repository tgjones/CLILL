#include "benchmark.h"

uint32_t benchmark_fibonacci(uint32_t number) {
	if (number <= 1)
		return 1;

	return benchmark_fibonacci(number - 1) + benchmark_fibonacci(number - 2);
}

int main()
{
	printf("%u", benchmark_fibonacci(35));
    return 0;
}