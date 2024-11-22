#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <stdio.h>

#define MALLOC(size, alignment) malloc(size)
#define FREE(pointer) free(pointer)

#ifdef __cplusplus
#define STRUCT_INIT(x) x
#else
#define STRUCT_INIT(x) (x)
#endif

#ifdef _MSC_VER
#define ALLOCA(type, name, length) type* name = (type*)_alloca(sizeof(type) * length)
#else
#define ALLOCA(type, name, length) type name[length]
#endif

typedef struct _Vector {
	float x, y, z;
} Vector;