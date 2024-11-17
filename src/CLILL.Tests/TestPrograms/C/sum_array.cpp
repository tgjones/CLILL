__attribute__((noinline))
int foo(int* nums, int count)
{
    auto result = 0;
    for (auto i = 0; i < count; i++)
    {
        result += nums[i];
    }
    return result;
}

int main()
{
    int nums[] = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    auto result = foo(nums, 10);
    return result;
}