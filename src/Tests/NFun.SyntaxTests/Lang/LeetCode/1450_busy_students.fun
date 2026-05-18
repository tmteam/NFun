# LeetCode 1450 — Number of Students Doing Their Homework
#
# Count i with startTime[i] ≤ queryTime ≤ endTime[i].

fun busyStudent(startTime, endTime, queryTime):
    count = 0
    i = 0
    while i < startTime.count():
        if startTime[i] <= queryTime and queryTime <= endTime[i]:
            count += 1
        i += 1
    return count

@Test
fun testCanonical():
    assertEqual(busyStudent([1, 2, 3], [3, 2, 7], 4), 1)

@Test
fun testAll():
    assertEqual(busyStudent([1, 2, 3], [3, 2, 7], 2), 2)

@Test
fun testNone():
    assertEqual(busyStudent([1, 2, 3], [3, 2, 7], 10), 0)
