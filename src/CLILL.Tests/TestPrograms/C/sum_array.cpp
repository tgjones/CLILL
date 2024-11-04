int foo(int* nums, int count)
{
    auto result = 0;
    for (auto i = 0; i < count; i++)
    {
        result += nums[i];
    }
    return result;
}