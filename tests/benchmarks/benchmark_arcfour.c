#include "benchmark.h"

inline static int benchmark_arcfour_key_setup(uint8_t* state, uint8_t* key, int length) {
	int i, j;
	uint8_t t;

	for (i = 0; i < 256; ++i) {
		state[i] = (uint8_t)i;
	}

	for (i = 0, j = 0; i < 256; ++i) {
		j = (j + state[i] + key[i % length]) % 256;
		t = state[i];
		state[i] = state[j];
		state[j] = t;
	}

	return i;
}

inline static int benchmark_arcfour_generate_stream(uint8_t* state, uint8_t* buffer, int length) {
	int i, j;
	int idx;
	uint8_t t;

	for (idx = 0, i = 0, j = 0; idx < length; ++idx) {
		i = (i + 1) % 256;
		j = (j + state[i]) % 256;
		t = state[i];
		state[i] = state[j];
		state[j] = t;
		buffer[idx] = state[(state[i] + state[j]) % 256];
	}

	return i;
}

int benchmark_arcfour(uint32_t iterations) {
	const int keyLength = 5;
	const int streamLength = 10;

	uint8_t* state = (uint8_t*)MALLOC(256, 8);
	uint8_t* buffer = (uint8_t*)MALLOC(64, 8);
	uint8_t key[keyLength];
	uint8_t stream[streamLength];

	key[0] = 0xDB;
	key[1] = 0xB7;
	key[2] = 0x60;
	key[3] = 0xD4;
	key[4] = 0x56;

	stream[0] = 0xEB;
	stream[1] = 0x9F;
	stream[2] = 0x77;
	stream[3] = 0x81;
	stream[4] = 0xB7;
	stream[5] = 0x34;
	stream[6] = 0xCA;
	stream[7] = 0x72;
	stream[8] = 0xA7;
	stream[9] = 0x19;

	int idx = 0;

	for (uint32_t i = 0; i < iterations; i++) {
		idx = benchmark_arcfour_key_setup(state, key, keyLength);
		idx = benchmark_arcfour_generate_stream(state, buffer, streamLength);
	}

	FREE(state);
	FREE(buffer);

	return idx;
}

int main()
{
	printf("%u", benchmark_arcfour(1000000));
    return 0;
}