#include <stdio.h>
struct str {
  int  member1;
};

struct str str1;
int main() {
  struct str str2;
  str1.member1 = 1;
  str2 = str1;

  if(str2.member1 == 1){
    printf("OK\n");
  }else{
    printf("NG\n");
  }
}
