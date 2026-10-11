"""Tiny pixel canvas working on palette indices (0 transparent, 1 light, 2 accent, 3 dark).

Everything is integer, aliased and flat: no gradients, no antialiasing.
"""


def rows(text):
    lines = [l for l in text.strip("\n").split("\n")]
    w = max(len(l) for l in lines)
    return [l.ljust(w, ".") for l in lines]


class Canvas:
    def __init__(self, w, h):
        self.w = w
        self.h = h
        self.px = [[0] * w for _ in range(h)]

    # -- basic access -------------------------------------------------
    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def set(self, x, y, c):
        if c is not None and self.inside(x, y):
            self.px[y][x] = c

    def get(self, x, y):
        return self.px[y][x] if self.inside(x, y) else 0

    def sx(self, x):
        """Mirror x around the vertical axis."""
        return self.w - 1 - x

    # -- primitives ---------------------------------------------------
    def rect(self, x0, y0, x1, y1, fill, line=None):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                edge = x in (x0, x1) or y in (y0, y1)
                self.set(x, y, line if (edge and line is not None) else fill)

    def hline(self, x0, x1, y, c):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            self.set(x, y, c)

    def vline(self, x, y0, y1, c):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            self.set(x, y, c)

    def line(self, x0, y0, x1, y1, c):
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.set(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def _mask_shape(self, test, x0, y0, x1, y1):
        m = set()
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if test(x, y):
                    m.add((x, y))
        return m

    def paint_mask(self, mask, fill, line=None):
        for (x, y) in mask:
            edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            self.set(x, y, line if (edge and line is not None) else fill)

    def ellipse_mask(self, cx, cy, rx, ry):
        """cx, cy may be .5 to get even-sized shapes."""
        def t(x, y):
            return ((x - cx) / (rx + 0.5)) ** 2 + ((y - cy) / (ry + 0.5)) ** 2 <= 1.0
        return self._mask_shape(t, int(cx - rx - 1), int(cy - ry - 1), int(cx + rx + 1), int(cy + ry + 1))

    def ellipse(self, cx, cy, rx, ry, fill, line=None):
        self.paint_mask(self.ellipse_mask(cx, cy, rx, ry), fill, line)

    def poly_mask(self, pts):
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        n = len(pts)

        def t(x, y):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = n - 1
            for i in range(n):
                xi, yi = pts[i][0] + 0.5, pts[i][1] + 0.5
                xj, yj = pts[j][0] + 0.5, pts[j][1] + 0.5
                if (yi > py) != (yj > py):
                    xint = xi + (py - yi) * (xj - xi) / (yj - yi)
                    if px < xint:
                        inside = not inside
                j = i
            return inside
        m = self._mask_shape(t, min(xs), min(ys), max(xs), max(ys))
        for i in range(n):
            a, b = pts[i], pts[(i + 1) % n]
            self._line_into(m, a[0], a[1], b[0], b[1])
        return m

    def _line_into(self, m, x0, y0, x1, y1):
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            m.add((x0, y0))
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def poly(self, pts, fill, line=None):
        self.paint_mask(self.poly_mask(pts), fill, line)

    def poly_sym(self, half_pts, fill, line=None):
        """Symmetric polygon given its left half (top to bottom)."""
        pts = list(half_pts) + [(self.sx(x), y) for (x, y) in reversed(half_pts)]
        self.poly(pts, fill, line)

    def stamp(self, text, x, y, flip=False, remap=None):
        """'.' = keep, '0' = erase, '1'..'3' = color. remap: {'2': 3}."""
        rr = rows(text) if isinstance(text, str) else text
        for j, r in enumerate(rr):
            if flip:
                r = r[::-1]
            for i, ch in enumerate(r):
                if ch == ".":
                    continue
                v = int(ch) if ch in "0123" else None
                if remap and ch in remap:
                    v = remap[ch]
                if v is not None:
                    self.set(x + i, y + j, v)

    def stamp_sym(self, text, x, y, remap=None):
        """Stamp at x and mirrored on the other side."""
        rr = rows(text)
        w = len(rr[0])
        self.stamp(rr, x, y, remap=remap)
        self.stamp(rr, self.w - x - w, y, flip=True, remap=remap)

    def outline(self, c=3):
        """Adds an outer outline around every non-transparent pixel (4-neighbourhood)."""
        add = []
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] == 0:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        if self.get(x + dx, y + dy) not in (0,):
                            add.append((x, y))
                            break
        for x, y in add:
            self.px[y][x] = c

    def replace(self, a, b, region=None):
        x0, y0, x1, y1 = region or (0, 0, self.w - 1, self.h - 1)
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.get(x, y) == a:
                    self.set(x, y, b)

    def shift(self, dx, dy):
        n = [[0] * self.w for _ in range(self.h)]
        for y in range(self.h):
            for x in range(self.w):
                if self.inside(x - dx, y - dy):
                    n[y][x] = self.px[y - dy][x - dx]
        self.px = n

    def bbox(self):
        xs = [x for y in range(self.h) for x in range(self.w) if self.px[y][x]]
        ys = [y for y in range(self.h) for x in range(self.w) if self.px[y][x]]
        if not xs:
            return None
        return min(xs), min(ys), max(xs), max(ys)

    def ground(self, bottom):
        """Move content so its lowest pixel sits on row `bottom`, centered horizontally."""
        b = self.bbox()
        if not b:
            return
        dx = ((self.w - 1) - (b[0] + b[2])) // 2
        self.shift(dx, bottom - b[3])

    def to_text(self):
        return "\n".join("".join("." if v == 0 else str(v) for v in r) for r in self.px)
