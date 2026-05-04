# 17_structs.fun - struct operations and algorithms

fun manhattan(p1, p2):
    return abs(p1.x - p2.x) + abs(p1.y - p2.y)

@Test
fun testManhattan():
    a = {x = 0, y = 0}
    b = {x = 3, y = 4}
    assertEqual(manhattan(a, b), 7)

fun dot(a, b):
    return a.x * b.x + a.y * b.y

@Test
fun testDot():
    a = {x = 1, y = 2}
    b = {x = 3, y = 4}
    assertEqual(dot(a, b), 11)

fun translate(p, dx, dy):
    p.x += dx
    p.y += dy
    return p

@Test
fun testTranslate():
    p = {x = 1, y = 2}
    q = translate(p, 10, 20)
    assertEqual(q.x, 11)
    assertEqual(q.y, 22)

fun makePoint(x, y):
    return {x = x, y = y}

fun midpoint(p1, p2):
    return makePoint((p1.x + p2.x) // 2, (p1.y + p2.y) // 2)

@Test
fun testMidpoint():
    a = makePoint(0, 0)
    b = makePoint(10, 20)
    m = midpoint(a, b)
    assertEqual(m.x, 5)
    assertEqual(m.y, 10)

fun swapFields(s):
    tmp = s.x
    s.x = s.y
    s.y = tmp
    return s

@Test
fun testSwapFields():
    p = {x = 10, y = 20}
    q = swapFields(p)
    assertEqual(q.x, 20)
    assertEqual(q.y, 10)
