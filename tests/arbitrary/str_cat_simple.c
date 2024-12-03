#include <stdio.h>
#include <string.h>
#include <stdlib.h>

int main(){
  const char *x = "hello";
  const char *y = " goodbye";
  size_t message_len = strlen(x) + 1; // 6
  char *message = (char*) calloc(message_len, 1); // 6 bytes
  strncat(message, x, message_len); // message contains "hello\n"
  message_len += strlen(y); // 6 + 8 = 14
  message = (char*) realloc(message, message_len); // message is now 14 bytes
  strncat(message, y, message_len);
  puts(message);
  free(message);
}