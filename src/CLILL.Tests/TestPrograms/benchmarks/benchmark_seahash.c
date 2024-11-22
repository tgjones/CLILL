#include "benchmark.h"

inline static uint64_t benchmark_seahash_read(uint8_t* pointer) {
	return *(uint64_t*)pointer;
}

inline static uint64_t benchmark_seahash_diffuse(uint64_t value) {
	value *= 0x6EED0E9DA4D94A4F;
	value ^= ((value >> 32) >> (int)(value >> 60));
	value *= 0x6EED0E9DA4D94A4F;

	return value;
}

static uint64_t benchmark_seahash_compute(uint8_t* buffer, int length, uint64_t a, uint64_t b, uint64_t c, uint64_t d) {
	const int blockSize = 32;

	int end = length & ~(blockSize - 1);

	for (int i = 0; i < end; i += blockSize) {
		a ^= benchmark_seahash_read(buffer + i);
		b ^= benchmark_seahash_read(buffer + i + 8);
		c ^= benchmark_seahash_read(buffer + i + 16);
		d ^= benchmark_seahash_read(buffer + i + 24);

		a = benchmark_seahash_diffuse(a);
		b = benchmark_seahash_diffuse(b);
		c = benchmark_seahash_diffuse(c);
		d = benchmark_seahash_diffuse(d);
	}

	int excessive = length - end;
	uint8_t* bufferEnd = buffer + end;

	if (excessive > 0) {
		a ^= benchmark_seahash_read(bufferEnd);

		if (excessive > 8) {
			b ^= benchmark_seahash_read(bufferEnd);

			if (excessive > 16) {
				c ^= benchmark_seahash_read(bufferEnd);

				if (excessive > 24) {
					d ^= benchmark_seahash_read(bufferEnd);
					d = benchmark_seahash_diffuse(d);
				}

				c = benchmark_seahash_diffuse(c);
			}

			b = benchmark_seahash_diffuse(b);
		}

		a = benchmark_seahash_diffuse(a);
	}

	a ^= b;
	c ^= d;
	a ^= c;
	a ^= (uint64_t)length;

	return benchmark_seahash_diffuse(a);
}

uint64_t benchmark_seahash(uint32_t iterations) {
	const int bufferLength = 1024 * 128;

	uint8_t* buffer = (uint8_t*)MALLOC(bufferLength, 8);

	for (int i = 0; i < bufferLength; i++) {
		buffer[i] = (uint8_t)(i % 256);
	}

	uint64_t hash = 0;

	for (uint32_t i = 0; i < iterations; i++) {
		hash = benchmark_seahash_compute(buffer, bufferLength, 0x16F11FE89B0D677C, 0xB480A793D8E6C86C, 0x6FE2E5AAF078EBC9, 0x14F994A4C5259381);
	}

	FREE(buffer);

	return hash;
}

int main()
{
	printf("%lu", benchmark_seahash(10000));
    return 0;
}