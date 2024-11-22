#include "benchmark.h"

typedef struct _Boid {
	Vector position, velocity, acceleration;
} Boid;

static uint32_t parkMiller;
static float maxSpeed;
static float maxForce;
static float separationDistance;
static float neighbourDistance;

inline static void benchmark_fireflies_flocking_add(Vector* left, const Vector* right) {
	left->x += right->x;
	left->y += right->y;
	left->z += right->z;
}

inline static void benchmark_fireflies_flocking_subtract(Vector* left, const Vector* right) {
	left->x -= right->x;
	left->y -= right->y;
	left->z -= right->z;
}

inline static void benchmark_fireflies_flocking_divide(Vector* vector, float value) {
	vector->x /= value;
	vector->y /= value;
	vector->z /= value;
}

inline static void benchmark_fireflies_flocking_multiply(Vector* vector, float value) {
	vector->x *= value;
	vector->y *= value;
	vector->z *= value;
}

inline static void benchmark_fireflies_flocking_normalize(Vector* vector) {
	float length = sqrtf(vector->x * vector->x + vector->y * vector->y + vector->z * vector->z);

	vector->x /= length;
	vector->y /= length;
	vector->z /= length;
}

inline static float benchmark_fireflies_flocking_length(Vector* vector) {
	return sqrtf(vector->x * vector->x + vector->y * vector->y + vector->z * vector->z);
}

inline static float benchmark_fireflies_flocking_random(void) {
	parkMiller = (uint32_t)(((uint64_t)parkMiller * 48271u) % 0x7fffffff);

	return parkMiller / 10000000.0f;
}

float benchmark_fireflies_flocking(uint32_t boids, uint32_t lifetime) {
	parkMiller = 666;
	maxSpeed = 1.0f;
	maxForce = 0.03f;
	separationDistance = 15.0f;
	neighbourDistance = 30.0f;

	Boid* fireflies = (Boid*)MALLOC(boids * sizeof(Boid), 16);

	for (int i = 0; i < boids; ++i) {
		fireflies[i].position = STRUCT_INIT(Vector) { benchmark_fireflies_flocking_random(), benchmark_fireflies_flocking_random(), benchmark_fireflies_flocking_random() };
		fireflies[i].velocity = STRUCT_INIT(Vector) { benchmark_fireflies_flocking_random(), benchmark_fireflies_flocking_random(), benchmark_fireflies_flocking_random() };
		fireflies[i].acceleration = STRUCT_INIT(Vector) { 0.0f, 0.0f, 0.0f };
	}

	for (int i = 0; i < lifetime; ++i) {
		// Update
		for (int boid = 0; boid < boids; ++boid) {
			benchmark_fireflies_flocking_add(&fireflies[boid].velocity, &fireflies[boid].acceleration);

			float speed = benchmark_fireflies_flocking_length(&fireflies[boid].velocity);

			if (speed > maxSpeed) {
				benchmark_fireflies_flocking_divide(&fireflies[boid].velocity, speed);
				benchmark_fireflies_flocking_multiply(&fireflies[boid].velocity, maxSpeed);
			}

			benchmark_fireflies_flocking_add(&fireflies[boid].position, &fireflies[boid].velocity);
			benchmark_fireflies_flocking_multiply(&fireflies[boid].acceleration, maxSpeed);
		}

		// Separation
		for (int boid = 0; boid < boids; ++boid) {
			Vector separation = { 0 };
			int count = 0;

			for (int target = 0; target < boids; ++target) {
				Vector position = fireflies[boid].position;

				benchmark_fireflies_flocking_subtract(&position, &fireflies[target].position);

				float distance = benchmark_fireflies_flocking_length(&position);

				if (distance > 0.0f && distance < separationDistance) {
					benchmark_fireflies_flocking_normalize(&position);
					benchmark_fireflies_flocking_divide(&position, distance);

					separation = position;
					count++;
				}
			}

			if (count > 0) {
				benchmark_fireflies_flocking_divide(&separation, (float)count);
				benchmark_fireflies_flocking_normalize(&separation);
				benchmark_fireflies_flocking_multiply(&separation, maxSpeed);
				benchmark_fireflies_flocking_subtract(&separation, &fireflies[boid].velocity);

				float force = benchmark_fireflies_flocking_length(&separation);

				if (force > maxForce) {
					benchmark_fireflies_flocking_divide(&separation, force);
					benchmark_fireflies_flocking_multiply(&separation, maxForce);
				}

				benchmark_fireflies_flocking_multiply(&separation, 1.5f);
				benchmark_fireflies_flocking_add(&fireflies[boid].acceleration, &separation);
			}
		}

		// Cohesion
		for (int boid = 0; boid < boids; ++boid) {
			Vector cohesion = { 0 };
			int count = 0;

			for (int target = 0; target < boids; ++target) {
				Vector position = fireflies[boid].position;

				benchmark_fireflies_flocking_subtract(&position, &fireflies[target].position);

				float distance = benchmark_fireflies_flocking_length(&position);

				if (distance > 0.0f && distance < neighbourDistance) {
					cohesion = fireflies[boid].position;
					count++;
				}
			}

			if (count > 0) {
				benchmark_fireflies_flocking_divide(&cohesion, (float)count);
				benchmark_fireflies_flocking_subtract(&cohesion, &fireflies[boid].position);
				benchmark_fireflies_flocking_normalize(&cohesion);
				benchmark_fireflies_flocking_multiply(&cohesion, maxSpeed);
				benchmark_fireflies_flocking_subtract(&cohesion, &fireflies[boid].velocity);

				float force = benchmark_fireflies_flocking_length(&cohesion);

				if (force > maxForce) {
					benchmark_fireflies_flocking_divide(&cohesion, force);
					benchmark_fireflies_flocking_multiply(&cohesion, maxForce);
				}

				benchmark_fireflies_flocking_add(&fireflies[boid].acceleration, &cohesion);
			}
		}
	}

	FREE(fireflies);

	return parkMiller;
}

int main()
{
	printf("%f", benchmark_fireflies_flocking(1000, 100));
    return 0;
}