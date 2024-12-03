#include "benchmark.h"

static uint32_t classicRandom;

inline static int benchmark_radix_random(void) {
	classicRandom = (6253729 * classicRandom + 4396403);

	return (int)(classicRandom % 32767);
}

inline static int benchmark_radix_find_largest(int* array, int length) {
	int i;
	int largest = -1;

	for (i = 0; i < length; i++) {
		if (array[i] > largest)
			largest = array[i];
	}

	return largest;
}

static void benchmark_radix_sort(int* array, int length) {
	int i;
	ALLOCA(int, semiSorted, length);
	int significantDigit = 1;
	int largest = benchmark_radix_find_largest(array, length);

	while (largest / significantDigit > 0) {
		int bucket[10] = { 0 };

		for (i = 0; i < length; i++) {
			bucket[(array[i] / significantDigit) % 10]++;
		}

		for (i = 1; i < 10; i++) {
			bucket[i] += bucket[i - 1];
		}

		for (i = length - 1; i >= 0; i--) {
			semiSorted[--bucket[(array[i] / significantDigit) % 10]] = array[i];
		}

		for (i = 0; i < length; i++) {
			array[i] = semiSorted[i];
		}

		significantDigit *= 10;
	}
}

int benchmark_radix(uint32_t iterations) {
	classicRandom = 7525;

	const int arrayLength = 128;

	int* array = (int*)MALLOC(arrayLength * sizeof(int), 16);

	for (uint32_t a = 0; a < iterations; a++) {
		for (int b = 0; b < arrayLength; b++) {
			array[b] = benchmark_radix_random();
		}

		benchmark_radix_sort(array, arrayLength);
	}

	int head = array[0];

	FREE(array);

	return head;
}

int main()
{
	printf("%d", benchmark_radix(100000));
    return 0;
}