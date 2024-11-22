#include "benchmark.h"

uint32_t benchmark_sieve_of_eratosthenes(uint32_t iterations) {
	const int size = 1024;

	uint8_t flags[size];
	uint32_t a, b, c, prime, count = 0;

	for (a = 1; a <= iterations; a++) {
		count = 0;

		for (b = 0; b < size; b++) {
			flags[b] = 1; // True
		}

		for (b = 0; b < size; b++) {
			if (flags[b] == 1) {
				prime = b + b + 3;
				c = b + prime;

				while (c < size) {
					flags[c] = 0; // False
					c += prime;
				}

				count++;
			}
		}
	}

	return count;
}

int main()
{
	printf("%u", benchmark_sieve_of_eratosthenes(1000000));
    return 0;
}