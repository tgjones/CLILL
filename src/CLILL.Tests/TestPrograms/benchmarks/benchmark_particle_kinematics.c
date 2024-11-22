#include "benchmark.h"

typedef struct _Particle {
	float x, y, z, vx, vy, vz;
} Particle;

float benchmark_particle_kinematics(uint32_t quantity, uint32_t iterations) {
	Particle* particles = (Particle*)MALLOC(quantity * sizeof(Particle), 16);

	for (uint32_t i = 0; i < quantity; ++i) {
		particles[i].x = (float)i;
		particles[i].y = (float)(i + 1);
		particles[i].z = (float)(i + 2);
		particles[i].vx = 1.0f;
		particles[i].vy = 2.0f;
		particles[i].vz = 3.0f;
	}

	for (uint32_t a = 0; a < iterations; ++a) {
		for (uint32_t b = 0, c = quantity; b < c; ++b) {
			Particle* p = &particles[b];

			p->x += p->vx;
			p->y += p->vy;
			p->z += p->vz;
		}
	}

	Particle particle = { particles[0].x, particles[0].y, particles[0].z };

	FREE(particles);

	return particle.x + particle.y + particle.z;
}

int main()
{
	printf("%f", benchmark_particle_kinematics(1000, 1000000));
    return 0;
}