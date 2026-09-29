import random, math, sys, statistics
D=[(0,1),(0,-1),(1,0),(-1,0)]
SH=[(3,[(0,0)]),(5,[(0,0),(1,0)]),(4,[(0,0),(1,0),(2,0)]),(5,[(0,0),(1,0),(0,1)]),(2,[(0,0),(1,0),(2,0),(3,0)]),
    (3,[(0,0),(1,0),(0,1),(1,1)]),(3,[(0,0),(1,0),(2,0),(0,1)]),(2,[(0,0),(1,0),(2,0),(2,1)]),(3,[(0,0),(1,0),(2,0),(1,1)]),
    (2,[(0,0),(1,0),(1,1),(2,1)]),(2,[(1,0),(2,0),(0,1),(1,1)])]
def norm(c):
    mx=min(x for x,y in c); my=min(y for x,y in c); return tuple(sorted((x-mx,y-my) for x,y in c))
def rot(c,t):
    for _ in range(t): c=[(-y,x) for x,y in c]
    return norm(c)
def gen_zones(W,K,MN,MX):
    for _ in range(30):
        z={}; fr=[]; size=[0]*K; s=[]
        md=max(2,round(math.sqrt(W*W/K))-1)
        for _ in range(2000):
            if len(s)>=K: break
            c=(random.randrange(W),random.randrange(W))
            if all(abs(a-c[0])+abs(b-c[1])>=md for a,b in s): s.append(c)
        while len(s)<K:
            c=(random.randrange(W),random.randrange(W))
            if c not in s: s.append(c)
        for i,sd in enumerate(s):
            z[sd]=i; size[i]=1; fr.append([(sd[0]+dx,sd[1]+dy) for dx,dy in D])
        while len(z)<W*W:
            best=-1
            for i in range(K):
                fr[i]=[c for c in fr[i] if 0<=c[0]<W and 0<=c[1]<W]
                if not fr[i]: continue
                if best<0 or size[i]<size[best]: best=i
            if best<0: break
            c=fr[best].pop(random.randrange(len(fr[best])))
            if c in z: continue
            z[c]=best; size[best]+=1; fr[best]+=[(c[0]+dx,c[1]+dy) for dx,dy in D]
        if min(size)>=MN and max(size)<=MX: break
    return z
def paint(z,K,C):
    adj=[set() for _ in range(K)]
    for (x,y),a in z.items():
        for dx,dy in D:
            b=z.get((x+dx,y+dy))
            if b is not None and b!=a: adj[a].add(b)
    col=[None]*K; used=[0]*C
    for k in sorted(range(K),key=lambda k:-len(adj[k])):
        best=min(range(C),key=lambda c:(sum(col[n]==c for n in adj[k])*100+used[c],random.random()))
        col[k]=best; used[best]+=1
    return col,adj
def sim(W=8,K=6,MN=7,MX=14,C=4,TC=7,TR=3,weights=None,colorbias=True,maxsteps=3000,assist=0.0,rotate=False,thresh=1.0):
    z=gen_zones(W,K,MN,MX); col,adj=paint(z,K,C)
    zc=[[c for c in z if z[c]==k] for k in range(K)]
    occ={}  # cell -> (pieceid,color)
    pieces={} # id -> dict(cells set abs, color)
    tray=[]; pid=0; clears=0; placed_cells=0; wrongplace=0
    shapes=SH if weights is None else [(w,s) for w,(_,s) in zip(weights,SH)]
    tot=sum(w for w,_ in shapes)
    def randshape():
        r=random.randrange(tot)
        for w,s in shapes:
            if r<w: return rot(s,random.randrange(4))
            r-=w
    def randcolor():
        if not colorbias: return random.randrange(C)
        w=[2]*C
        for k in range(K): w[col[k]]+=sum(1 for c in zc[k] if c not in occ)
        r=random.randrange(sum(w))
        for c in range(C):
            if r<w[c]: return c
            r-=w[c]
    def fits_board(sh,anchor):
        cells=[(x+anchor[0],y+anchor[1]) for x,y in sh]
        return all(0<=a<W and 0<=b<W and (a,b) not in occ for a,b in cells), cells
    def good_spots(sh,color):
        out=[]
        for ax in range(W):
            for ay in range(W):
                ok,cells=fits_board(sh,(ax,ay))
                if ok and all(col[z[c]]==color for c in cells) and len({z[c] for c in cells})==1: out.append(cells)
        return out
    def any_spot(sh):
        for ax in range(W):
            for ay in range(W):
                ok,cells=fits_board(sh,(ax,ay))
                if ok: return cells
        return None
    def tray_cells(): return sum(len(s) for s,_ in tray)
    def zone_empty(k): return [c for c in zc[k] if c not in occ]
    def spot_score(cells):
        k=z[cells[0]]; rem=len(zone_empty(k))-len(cells)
        if rem==0: return 10000
        # prefer leaving remaining empty region connected & filling fuller zones
        emp=set(zone_empty(k))-set(cells)
        # count components
        seen=set(); comp=0; small=0
        for c in emp:
            if c in seen: continue
            comp+=1; st=[c]; seen.add(c); n=0
            while st:
                u=st.pop(); n+=1
                for dx,dy in D:
                    v=(u[0]+dx,u[1]+dy)
                    if v in emp and v not in seen: seen.add(v); st.append(v)
        return -rem*3 - comp*5
    def place(cells,color):
        nonlocal pid
        pieces[pid]={'cells':set(cells),'color':color}
        for c in cells: occ[c]=(pid,color)
        pid+=1
        return z[cells[0]]
    def check_clear(k):
        nonlocal clears
        if all(c in occ for c in zc[k]) and sum(occ[c][1]==col[k] for c in zc[k])>=thresh*len(zc[k]):
            for c in zc[k]:
                p=occ.pop(c)[0]; pieces[p]['cells'].discard(c)
                if not pieces[p]['cells']: del pieces[p]
            clears+=1
            opts=[c for c in range(C) if c!=col[k]]
            col[k]=min(opts,key=lambda c:(sum(col[n]==c for n in adj[k]),random.random()))
            return True
        return False
    steps=0
    while steps<maxsteps:
        steps+=1
        if not tray:
            for _ in range(3):
                if random.random()<assist:
                    opts=[]
                    for w,base in shapes:
                        for t in range(4):
                            sh=rot(base,t)
                            for c in range(C):
                                sp=good_spots(sh,c)
                                if sp:
                                    done=any(len(zone_empty(z[x[0]]))==len(x) for x in sp)
                                    opts.append((w*(4 if done else 1),sh,c))
                    if opts:
                        r=random.uniform(0,sum(o[0] for o in opts))
                        for o in opts:
                            if r<o[0]: tray.append((o[1],o[2])); break
                            r-=o[0]
                        else: tray.append((opts[-1][1],opts[-1][2]))
                        continue
                tray.append((randshape(),randcolor()))
        # 1. good placement from tray
        best=None
        for i,(sh,c) in enumerate(tray):
            for sh2 in ({rot(list(sh),t) for t in range(4)} if rotate else [sh]):
              for cells in good_spots(sh2,c):
                sc=spot_score(cells)
                if best is None or sc>best[0]: best=(sc,i,cells)
        # 2. relocate wrong-colored board pieces into good spots
        if best is None or best[0]<10000:
            for p,info in list(pieces.items()):
                cells=info['cells']
                if all(col[z[c]]==info['color'] for c in cells): continue
                mn=(min(x for x,y in cells),min(y for x,y in cells))
                sh=norm(list(cells))
                for c in cells: occ.pop(c)
                spots=good_spots(sh,info['color'])
                for c in cells: occ[c]=(p,info['color'])
                for sp in spots:
                    if set(sp)&cells: continue
                    sc=spot_score(sp)+1
                    if best is None or sc>best[0]: best=(sc,('move',p),sp)
        if best is not None:
            sc,i,cells=best
            if isinstance(i,tuple):
                p=i[1]; info=pieces[p]
                for c in info['cells']: occ.pop(c)
                info['cells']=set(cells)
                for c in cells: occ[c]=(p,info['color'])
                k=z[cells[0]]
            else:
                sh,c=tray.pop(i); k=place(cells,c); placed_cells+=len(cells)
            check_clear(k)
            continue
        # 3. no good move: dump a tray piece anywhere (least filled zone)
        dumped=False
        for i,(sh,c) in enumerate(sorted(tray,key=lambda t:len(t[0]))):
            cells=any_spot(sh)
            if cells:
                tray.remove((sh,c)); place(cells,c); wrongplace+=1; dumped=True; break
        if dumped: continue
        # 4. dead? (no tray piece fits; parking to tray: can board piece fit in tray free area?)
        free=TC*TR-tray_cells()
        if any(len(info['cells'])<=free and not all(col[z[c]]==info['color'] for c in info['cells']) for info in pieces.values()):
            # park smallest wrong piece into tray
            p=min((p for p,info in pieces.items() if not all(col[z[c]]==info['color'] for c in info['cells'])),key=lambda p:len(pieces[p]['cells']))
            info=pieces.pop(p)
            for c in info['cells']: occ.pop(c)
            tray.append((norm(list(info['cells'])),info['color']))
            continue
        break
    return clears, placed_cells, wrongplace, steps>=maxsteps
def run(n=200,**kw):
    res=[sim(**kw) for _ in range(n)]
    cl=[r[0] for r in res]
    q=sorted(cl)
    return f"clears avg {statistics.mean(cl):.1f} median {q[len(q)//2]} p10 {q[len(q)//10]} p90 {q[9*len(q)//10]} | maxed {sum(r[3] for r in res)} | dumps/game {statistics.mean(r[2] for r in res):.1f}"
if __name__ == '__main__':
  random.seed(1)
  cfg=sys.argv[1] if len(sys.argv)>1 else 'base'
  print(cfg, run(**eval(f"dict({sys.argv[2] if len(sys.argv)>2 else ''})")))
