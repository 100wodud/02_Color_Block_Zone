# 색 없는 조각 모드 시뮬레이션.
# 규칙: 조각은 색이 없고, 구역이 아무 조각으로나 꽉 차면 지워진다.
# 한 구역 안에만 놓인 조각의 칸은 "맞춤" 칸이다. 구역이 전부 맞춤 칸이면 PERFECT.
# 트레이는 7x3 실제 배치(TrayPacker 와 같은 방식), 판 조각을 트레이로 빼기 / 판 안 옮기기 허용.
import random, sys, statistics
from balance import SH, norm, rot, gen_zones, D

TC, TR = 7, 3

def tray_fits(cells, anchor, occ, gap):
    for x, y in cells:
        c = (x + anchor[0], y + anchor[1])
        if not (0 <= c[0] < TC and 0 <= c[1] < TR) or c in occ: return False
        if gap and any((c[0] + dx, c[1] + dy) in occ for dx, dy in D): return False
    return True

def tray_spot(shape, occ, gap=False):
    for t in range(4):
        s = rot(list(shape), t)
        for ax in range(TC):
            for ay in range(TR - 1, -1, -1):
                if tray_fits(s, (ax, ay), occ, gap): return s, (ax, ay)
    return None

def pack(shapes):
    for gap in (True, False):
        occ = set(); out = []
        def rec(i):
            if i == len(shapes): return True
            for t in range(4):
                s = rot(list(shapes[i]), t)
                for ax in range(TC):
                    for ay in range(TR - 1, -1, -1):
                        if not tray_fits(s, (ax, ay), occ, gap): continue
                        cs = {(x + ax, y + ay) for x, y in s}
                        occ.update(cs); out.append((s, cs))
                        if rec(i + 1): return True
                        out.pop(); occ.difference_update(cs)
            return False
        if rec(0): return out
    return None

def sim(W=8, K=6, MN=7, MX=14, policy='bonus', max_rounds=300, rotate=False, park=True, move=True, budget=None, refill=2, perfect_refill=None):
    z = gen_zones(W, K, MN, MX)
    zc = [[c for c in z if z[c] == k] for k in range(K)]
    occ = {}        # board cell -> piece id
    matched = {}    # board cell -> bool (그 조각이 한 구역 안에만 놓였나)
    pieces = {}     # id -> set of board cells
    tray = []       # [(shape, set of tray cells)]
    pid = 0
    st = dict(rounds=0, placed=0, clears=0, perfect=0, score=0, inzone=0, parks=0, moves=0, combo_max=0, end='cap')
    combo = 0
    left = [budget]  # 남은 놓기 횟수 (None 이면 무제한)
    tot = sum(w for w, _ in SH)

    def randshape():
        r = random.randrange(tot)
        for w, s in SH:
            if r < w: return rot(s, random.randrange(4))
            r -= w

    def tray_occ(skip=None):
        o = set()
        for i, (_, cs) in enumerate(tray):
            if i != skip: o |= cs
        return o

    def spots(shape, blocked=frozenset()):
        shapes = {rot(list(shape), t) for t in range(4)} if rotate else [tuple(shape)]
        out = []
        for s in shapes:
            for ax in range(W):
                for ay in range(W):
                    cells = [(x + ax, y + ay) for x, y in s]
                    if all(0 <= a < W and 0 <= b < W and ((a, b) not in occ or (a, b) in blocked) for a, b in cells):
                        out.append(cells)
        return out

    def score_spot(cells, ignore=frozenset()):
        zs = {z[c] for c in cells}
        filled = lambda k: sum(1 for c in zc[k] if (c in occ and c not in ignore) or c in cells)
        done = sum(1 for k in zs if filled(k) == len(zc[k]))
        single = len(zs) == 1
        # 빈 칸 조각내기 벌점
        emp = {c for k in zs for c in zc[k] if (c not in occ or c in ignore) and c not in cells}
        seen = set(); comps = 0
        for c in emp:
            if c in seen: continue
            comps += 1; stack = [c]; seen.add(c)
            while stack:
                u = stack.pop()
                for dx, dy in D:
                    v = (u[0] + dx, u[1] + dy)
                    if v in emp and v not in seen: seen.add(v); stack.append(v)
        s = done * 1000 + sum(filled(k) / len(zc[k]) for k in zs) * 10 - comps * 4 - len(zs) * 2
        if policy == 'bonus' and single: s += 30
        if policy == 'casual': s = random.random() * 20 + done * 1000
        return s

    def resolve(cells):
        nonlocal combo
        zs = {z[c] for c in cells}
        full = [k for k in zs if all(c in occ for c in zc[k])]
        if not full: combo = 0; return
        combo += 1; st['combo_max'] = max(st['combo_max'], combo)
        for k in full:
            m = sum(1 for c in zc[k] if matched[c])
            pts = (len(zc[k]) + m) * 10
            if m == len(zc[k]): pts *= 2; st['perfect'] += 1
            st['score'] += int(pts * (1 + (combo - 1) * 0.5)); st['clears'] += 1
            if left[0] is not None: left[0] += (perfect_refill if perfect_refill is not None and m == len(zc[k]) else refill)
            for c in zc[k]:
                p = occ.pop(c); matched.pop(c); pieces[p].discard(c)
                if not pieces[p]: del pieces[p]

    def put(cells, fresh):
        nonlocal pid
        single = len({z[c] for c in cells}) == 1
        pieces[pid] = set(cells)
        for c in cells: occ[c] = pid; matched[c] = single
        pid += 1
        if fresh:
            st['placed'] += 1; st['score'] += len(cells); st['inzone'] += single
        resolve(cells)

    def has_move_code():  # StageController.HasMove 와 같은 판정
        if any(spots(s) for s, _ in tray): return True
        o = tray_occ()
        return any(tray_spot(norm(list(cs)), o) for cs in pieces.values())

    stall = 0
    while st['rounds'] < max_rounds:
        if stall > 8: st['end'] = 'stall'; break
        if left[0] is not None and left[0] <= 0: st['end'] = 'moves'; break
        if not tray:
            st['rounds'] += 1
            shapes = [randshape() for _ in range(3)]
            packed = pack(shapes)
            tray = [(s, cs) for s, cs in packed]
            fresh = [True] * len(tray)
        # 1) 트레이 조각을 판에 바로 놓기
        best = None
        for i, (s, _) in enumerate(tray):
            for cells in spots(s):
                sc = score_spot(cells)
                if best is None or sc > best[0]: best = (sc, i, cells)
        if best:
            _, i, cells = best
            tray.pop(i); f = fresh.pop(i)
            if left[0] is not None: left[0] -= 1
            c0 = st['clears']; put(cells, f)
            stall = 0 if f or st['clears'] > c0 else stall + 1
            continue
        # 2) 판 조각 하나를 옮겨서(판 안 또는 트레이로) 자리가 생기면 그렇게 한다
        best = None
        for p, cs in (pieces.items() if move or park else []):
            blocked = frozenset(cs)
            for i, (s, _) in enumerate(tray):
                for cells in spots(s, blocked):
                    if not set(cells) & cs: continue
                    rest = cs - set(cells)
                    # 옮길 조각이 갈 곳: 판 다른 자리 or 트레이
                    sh = norm(list(cs))
                    for dst in (spots(sh, blocked) if move else []):
                        if set(dst) & set(cells): continue
                        sc = score_spot(cells, blocked) - 5
                        if best is None or sc > best[0]: best = (sc, 'move', p, dst, i, cells)
                        break
                    else:
                        o = tray_occ(skip=i)
                        if park and tray_spot(sh, o):
                            sc = score_spot(cells, blocked) - 20
                            if best is None or sc > best[0]: best = (sc, 'park', p, None, i, cells)
        if best:
            _, kind, p, dst, i, cells = best
            cs = pieces.pop(p)
            ms = {c: matched.pop(c) for c in cs}
            for c in cs: occ.pop(c)
            s, _ = tray.pop(i); f = fresh.pop(i)
            if left[0] is not None: left[0] -= 2
            if kind == 'move':
                st['moves'] += 1
                pieces[p] = set(dst)
                for c in dst: occ[c] = p; matched[c] = len({z[x] for x in dst}) == 1
            else:
                st['parks'] += 1
                sp = tray_spot(norm(list(cs)), tray_occ())
                tray.append((sp[0], {(x + sp[1][0], y + sp[1][1]) for x, y in sp[0]})); fresh.append(False)
            c0 = st['clears']; put(cells, f)
            if kind == 'move' and p in pieces: resolve(dst)
            stall = 0 if f or st['clears'] > c0 else stall + 1
            continue
        st['end'] = 'softlock' if park and has_move_code() else 'gameover'
        break
    st['minutes'] = (st['placed'] + st['moves']) * 3 / 60  # 한 수에 3초로 잡음
    return st

def run(n, **kw):
    rs = [sim(**kw) for _ in range(n)]
    g = lambda k: [r[k] for r in rs]
    q = sorted(g('placed'))
    ends = {e: sum(r['end'] == e for r in rs) for e in ('gameover', 'moves', 'softlock', 'stall', 'cap')}
    pl = sum(g('placed'))
    return (f"조각 중간값 {q[len(q)//2]} (p10 {q[len(q)//10]}, p90 {q[9*len(q)//10]}) | "
            f"구역/100조각 {100*sum(g('clears'))/pl:.1f} | 한구역배치 {100*sum(g('inzone'))/pl:.0f}% | "
            f"PERFECT {100*sum(g('perfect'))/max(1,sum(g('clears'))):.0f}% | "
            f"시간 중간값 {sorted(g('minutes'))[n//2]:.1f}분 | 점수 중간값 {sorted(g('score'))[n//2]} | 옮김/100조각 {100*(sum(g('moves'))+sum(g('parks')))/pl:.1f} | 끝 {ends}")

if __name__ == '__main__':
    random.seed(int(sys.argv[3]) if len(sys.argv) > 3 else 1)
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 50
    print(run(n, **eval(f"dict({sys.argv[2] if len(sys.argv) > 2 else ''})")))
