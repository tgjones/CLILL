#include "benchmark.h"

typedef enum _PixarRayHit {
	PIXAR_RAYTRACER_NONE = 0,
	PIXAR_RAYTRACER_LETTER = 1,
	PIXAR_RAYTRACER_WALL = 2,
	PIXAR_RAYTRACER_SUN = 3
} PixarRayHit;

static uint32_t marsagliaZ, marsagliaW;

inline static Vector benchmark_pixar_raytracer_multiply(Vector left, Vector right) {
	left.x *= right.x;
	left.y *= right.y;
	left.z *= right.z;

	return left;
}

inline static Vector benchmark_pixar_raytracer_multiply_float(Vector vector, float value) {
	vector.x *= value;
	vector.y *= value;
	vector.z *= value;

	return vector;
}

inline static float benchmark_pixar_raytracer_modulus(Vector left, Vector right) {
	return left.x * right.x + left.y * right.y + left.z * right.z;
}

inline static float benchmark_pixar_raytracer_modulus_self(Vector vector) {
	return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
}

inline static Vector benchmark_pixar_raytracer_inverse(Vector vector) {
	return benchmark_pixar_raytracer_multiply_float(vector, 1 / sqrtf(benchmark_pixar_raytracer_modulus_self(vector)));
}

inline static Vector benchmark_pixar_raytracer_add(Vector left, Vector right) {
	left.x += right.x;
	left.y += right.y;
	left.z += right.z;

	return left;
}

inline static Vector benchmark_pixar_raytracer_add_float(Vector vector, float value) {
	vector.x += value;
	vector.y += value;
	vector.z += value;

	return vector;
}

inline static Vector benchmark_pixar_raytracer_cross(Vector to, Vector from) {
	Vector vector = { 0 };

	vector.x = to.y * from.z - to.z * from.y;
	vector.y = to.z * from.x - to.x * from.z;
	vector.z = to.x * from.y - to.y * from.x;

	return vector;
}

inline static float benchmark_pixar_raytracer_min(float left, float right) {
	return left < right ? left : right;
}

inline static float benchmark_pixar_raytracer_box_test(Vector position, Vector lowerLeft, Vector upperRight) {
	lowerLeft = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(position, lowerLeft), -1);
	upperRight = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(upperRight, position), -1);

	return -benchmark_pixar_raytracer_min(benchmark_pixar_raytracer_min(benchmark_pixar_raytracer_min(lowerLeft.x, upperRight.x), benchmark_pixar_raytracer_min(lowerLeft.y, upperRight.y)), benchmark_pixar_raytracer_min(lowerLeft.z, upperRight.z));
}

inline static float benchmark_pixar_raytracer_random(void) {
	marsagliaZ = 36969 * (marsagliaZ & 65535) + (marsagliaZ >> 16);
	marsagliaW = 18000 * (marsagliaW & 65535) + (marsagliaW >> 16);

	return ((marsagliaZ << 16) + marsagliaW) * 2.0f / 10000000000.0f;
}

static float benchmark_pixar_raytracer_sample(Vector position, int* hitType) {
	const int size = 60;

	float distance = 1e9f;
	Vector f = position;
	uint8_t letters[size];

	// P              // I              // X              // A              // R
	letters[0] = 53; letters[12] = 65; letters[24] = 73; letters[32] = 85; letters[44] = 97; letters[56] = 99;
	letters[1] = 79; letters[13] = 79; letters[25] = 79; letters[33] = 79; letters[45] = 79; letters[57] = 87;
	letters[2] = 53; letters[14] = 69; letters[26] = 81; letters[34] = 89; letters[46] = 97; letters[58] = 105;
	letters[3] = 95; letters[15] = 79; letters[27] = 95; letters[35] = 95; letters[47] = 95; letters[59] = 79;
	letters[4] = 53; letters[16] = 67; letters[28] = 73; letters[36] = 89; letters[48] = 97;
	letters[5] = 87; letters[17] = 79; letters[29] = 95; letters[37] = 95; letters[49] = 87;
	letters[6] = 57; letters[18] = 67; letters[30] = 81; letters[38] = 93; letters[50] = 101;
	letters[7] = 87; letters[19] = 95; letters[31] = 79; letters[39] = 79; letters[51] = 87;
	letters[8] = 53; letters[20] = 65;                   letters[40] = 87; letters[52] = 97;
	letters[9] = 95; letters[21] = 95;                   letters[41] = 87; letters[53] = 95;
	letters[10] = 57; letters[22] = 69;                   letters[42] = 91; letters[54] = 101;
	letters[11] = 95; letters[23] = 95;                   letters[43] = 87; letters[55] = 95;

	f.z = 0.0f;

	for (int i = 0; i < size; i += 4) {
		Vector begin = benchmark_pixar_raytracer_multiply_float(STRUCT_INIT(Vector) { letters[i] - 79.0f, letters[i + 1] - 79.0f, 0.0f }, 0.5f);
		Vector e = benchmark_pixar_raytracer_add(benchmark_pixar_raytracer_multiply_float(STRUCT_INIT(Vector) { letters[i + 2] - 79.0f, letters[i + 3] - 79.0f, 0.0f }, 0.5f), benchmark_pixar_raytracer_multiply_float(begin, -1.0f));
		Vector o = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(f, benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(begin, e), benchmark_pixar_raytracer_min(-benchmark_pixar_raytracer_min(benchmark_pixar_raytracer_modulus(benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(begin, f), -1.0f), e) / benchmark_pixar_raytracer_modulus_self(e), 0.0f), 1.0f))), -1.0f);

		distance = benchmark_pixar_raytracer_min(distance, benchmark_pixar_raytracer_modulus_self(o));
	}

	distance = sqrtf(distance);

	Vector curves[2] = { 0 };

	curves[0] = STRUCT_INIT(Vector) { -11.0f, 6.0f, 0.0f };
	curves[1] = STRUCT_INIT(Vector) { 11.0f, 6.0f, 0.0f };

	for (int i = 1; i >= 0; i--) {
		Vector o = benchmark_pixar_raytracer_add(f, benchmark_pixar_raytracer_multiply_float(curves[i], -1.0f));
		float m = 0.0f;

		if (o.x > 0.0f) {
			m = fabsf(sqrtf(benchmark_pixar_raytracer_modulus_self(o)) - 2.0f);
		}
		else {
			if (o.y > 0.0f)
				o.y += -2.0f;
			else
				o.y += 2.0f;

			o.y += sqrtf(benchmark_pixar_raytracer_modulus_self(o));
		}

		distance = benchmark_pixar_raytracer_min(distance, m);
	}

	distance = powf(powf(distance, 8.0f) + powf(position.z, 8.0f), 0.125f) - 0.5f;
	*hitType = PIXAR_RAYTRACER_LETTER;

	float roomDistance = benchmark_pixar_raytracer_min(-benchmark_pixar_raytracer_min(benchmark_pixar_raytracer_box_test(position, STRUCT_INIT(Vector) { -30.0f, -0.5f, -30.0f }, STRUCT_INIT(Vector) { 30.0f, 18.0f, 30.0f }), benchmark_pixar_raytracer_box_test(position, STRUCT_INIT(Vector) { -25.0f, -17.5f, -25.0f }, STRUCT_INIT(Vector) { 25.0f, 20.0f, 25.0f })), benchmark_pixar_raytracer_box_test(STRUCT_INIT(Vector) { fmodf(fabsf(position.x), 8), position.y, position.z }, STRUCT_INIT(Vector) { 1.5f, 18.5f, -25.0f }, STRUCT_INIT(Vector) { 6.5f, 20.0f, 25.0f }));

	if (roomDistance < distance) {
		distance = roomDistance;
		*hitType = PIXAR_RAYTRACER_WALL;
	}

	float sun = 19.9f - position.y;

	if (sun < distance) {
		distance = sun;
		*hitType = PIXAR_RAYTRACER_SUN;
	}

	return distance;
}

static int benchmark_pixar_raytracer_ray_marching(Vector origin, Vector direction, Vector* hitPosition, Vector* hitNormal) {
	int hitType = PIXAR_RAYTRACER_NONE;
	int noHitCount = 0;
	float distance = 0.0f;

	for (float i = 0; i < 100; i += distance) {
		*hitPosition = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(origin, direction), i);
		distance = benchmark_pixar_raytracer_sample(*hitPosition, &hitType);

		if (distance < 0.01f || ++noHitCount > 99) {
			*hitNormal = benchmark_pixar_raytracer_inverse(STRUCT_INIT(Vector) { benchmark_pixar_raytracer_sample(benchmark_pixar_raytracer_add(*hitPosition, STRUCT_INIT(Vector) { 0.01f, 0.0f, 0.0f }), &noHitCount) - distance, benchmark_pixar_raytracer_sample(benchmark_pixar_raytracer_add(*hitPosition, STRUCT_INIT(Vector) { 0.0f, 0.01f, 0.0f }), &noHitCount) - distance, benchmark_pixar_raytracer_sample(benchmark_pixar_raytracer_add(*hitPosition, STRUCT_INIT(Vector) { 0.0f, 0.0f, 0.01f }), &noHitCount) - distance });

			return hitType;
		}
	}

	return PIXAR_RAYTRACER_NONE;
}

static Vector benchmark_pixar_raytracer_trace(Vector origin, Vector direction) {
	Vector
		sampledPosition = { 1.0f, 1.0f, 1.0f },
		normal = { 1.0f, 1.0f, 1.0f },
		color = { 1.0f, 1.0f, 1.0f },
		attenuation = { 1.0f, 1.0f, 1.0f },
		lightDirection = benchmark_pixar_raytracer_inverse(STRUCT_INIT(Vector) { 0.6f, 0.6f, 1.0f });

	for (int bounce = 3; bounce > 0; bounce--) {
		int hitType = benchmark_pixar_raytracer_ray_marching(origin, direction, &sampledPosition, &normal);

		switch (hitType) {
		case PIXAR_RAYTRACER_NONE:
			break;

		case PIXAR_RAYTRACER_LETTER: {
			direction = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(direction, normal), benchmark_pixar_raytracer_modulus(normal, direction) * -2.0f);
			origin = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(sampledPosition, direction), 0.1f);
			attenuation = benchmark_pixar_raytracer_multiply_float(attenuation, 0.2f);

			break;
		}

		case PIXAR_RAYTRACER_WALL: {
			float
				incidence = benchmark_pixar_raytracer_modulus(normal, lightDirection),
				p = 6.283185f * benchmark_pixar_raytracer_random(),
				c = benchmark_pixar_raytracer_random(),
				s = sqrtf(1.0f - c),
				g = normal.z < 0 ? -1.0f : 1.0f,
				u = -1.0f / (g + normal.z),
				v = normal.x * normal.y * u;

			direction = benchmark_pixar_raytracer_add(benchmark_pixar_raytracer_add(STRUCT_INIT(Vector) { v, g + normal.y * normal.y * u, -normal.y * (cosf(p) * s) }, STRUCT_INIT(Vector) { 1.0f + g * normal.x * normal.x * u, g* v, -g * normal.x }), benchmark_pixar_raytracer_multiply_float(normal, sqrtf(c)));
			origin = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(sampledPosition, direction), 0.1f);
			attenuation = benchmark_pixar_raytracer_multiply_float(attenuation, 0.2f);

			if (incidence > 0 && benchmark_pixar_raytracer_ray_marching(benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(sampledPosition, normal), 0.1f), lightDirection, &sampledPosition, &normal) == PIXAR_RAYTRACER_SUN)
				color = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_multiply(benchmark_pixar_raytracer_add(color, attenuation), STRUCT_INIT(Vector) { 500.0f, 400.0f, 100.0f }), incidence);

			break;
		}

		case PIXAR_RAYTRACER_SUN: {
			color = benchmark_pixar_raytracer_multiply(benchmark_pixar_raytracer_add(color, attenuation), STRUCT_INIT(Vector) { 50.0f, 80.0f, 100.0f });

			goto escape;
		}
		}
	}

escape:

	return color;
}

float benchmark_pixar_raytracer(uint32_t width, uint32_t height, uint32_t samples) {
	marsagliaZ = 666;
	marsagliaW = 999;

	Vector position = { -22.0f, 5.0f, 25.0f };
	Vector goal = { -3.0f, 4.0f, 0.0f };

	goal = benchmark_pixar_raytracer_add(benchmark_pixar_raytracer_inverse(goal), benchmark_pixar_raytracer_multiply_float(position, -1.0f));

	Vector left = { goal.z, 0, goal.x };

	left = benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_inverse(left), 1.0f / width);

	Vector up = benchmark_pixar_raytracer_cross(goal, left);
	Vector color = { 0 };
	Vector adjust = { 0 };

	for (uint32_t y = height; y > 0; y--) {
		for (uint32_t x = width; x > 0; x--) {
			for (uint32_t p = samples; p > 0; p--) {
				color = benchmark_pixar_raytracer_add(color, benchmark_pixar_raytracer_trace(position, benchmark_pixar_raytracer_add(benchmark_pixar_raytracer_inverse(benchmark_pixar_raytracer_multiply_float(benchmark_pixar_raytracer_add(goal, left), x - width / 2 + benchmark_pixar_raytracer_random())), benchmark_pixar_raytracer_multiply_float(up, y - height / 2 + benchmark_pixar_raytracer_random()))));
			}

			color = benchmark_pixar_raytracer_multiply_float(color, (1.0f / samples) + 14.0f / 241.0f);
			adjust = benchmark_pixar_raytracer_add_float(color, 1.0f);
			color = STRUCT_INIT(Vector) {
				color.x / adjust.x,
					color.y / adjust.y,
					color.z / adjust.z
			};

			color = benchmark_pixar_raytracer_multiply_float(color, 255.0f);
		}
	}

	return color.x + color.y + color.z;
}

int main()
{
	printf("%f", benchmark_pixar_raytracer(90, 60, 4));
    return 0;
}