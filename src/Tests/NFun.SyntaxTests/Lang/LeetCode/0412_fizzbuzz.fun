# LeetCode 0412 — Fizz Buzz
#
# For i in 1..n produce 'Fizz' on multiples of 3, 'Buzz' on multiples of 5,
# 'FizzBuzz' on multiples of 15, otherwise the number as text.

fun fizz(i):
    if i % 15 == 0: return 'FizzBuzz'
    if i % 3 == 0: return 'Fizz'
    if i % 5 == 0: return 'Buzz'
    return '{i}'

fun fizzBuzz(n):
    result = []
    i = 1
    while i <= n:
        result = concat(result, [fizz(i)])
        i += 1
    return result

@Test
fun testThree():
    assertEqual(fizzBuzz(3), ['1', '2', 'Fizz'])

@Test
fun testFifteen():
    assertEqual(fizzBuzz(15)[14], 'FizzBuzz')

@Test
fun testFive():
    assertEqual(fizzBuzz(5), ['1', '2', 'Fizz', '4', 'Buzz'])
