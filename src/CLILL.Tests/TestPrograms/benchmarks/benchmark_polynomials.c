#include "benchmark.h"

float benchmark_polynomials(uint32_t iterations) {
	const float x = 0.2f;

	float pu = 0.0f;
	float poly[100] = { 0 };

	for (uint32_t i = 0; i < iterations; i++) {
		float mu = 10.0f;
		float s;
		int j;

		for (j = 0; j < 100; j++) {
			poly[j] = mu = (mu + 2.0f) / 2.0f;
		}

		s = 0.0f;

		for (j = 0; j < 100; j++) {
			s = x * s + poly[j];
		}

		pu += s;
	}

	return pu;
}

int main()
{
	printf("%f", benchmark_polynomials(1000000));
    return 0;
}