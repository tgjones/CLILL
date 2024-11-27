#include "benchmark.h"

float benchmark_mandelbrot(uint32_t width, uint32_t height, uint32_t iterations) {
	float data = 0.0f;

	for (uint32_t i = 0; i < iterations; i++) {
		float
			left = -2.1f,
			right = 1.0f,
			top = -1.3f,
			bottom = 1.3f,
			deltaX = (right - left) / width,
			deltaY = (bottom - top) / height,
			coordinateX = left;

		for (uint32_t x = 0; x < width; x++) {
			float coordinateY = top;

			for (uint32_t y = 0; y < height; y++) {
				float workX = 0;
				float workY = 0;
				int counter = 0;

				while (counter < 255 && sqrtf((workX * workX) + (workY * workY)) < 2.0f) {
					counter++;

					float newX = (workX * workX) - (workY * workY) + coordinateX;

					workY = 2 * workX * workY + coordinateY;
					workX = newX;
				}

				data = workX + workY;
				coordinateY += deltaY;
			}

			coordinateX += deltaX;
		}
	}

	return data;
}

int main()
{
	printf("%f", benchmark_mandelbrot(1920, 1080, 7));
    return 0;
}