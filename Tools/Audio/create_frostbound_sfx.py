"""Generate original, deterministic frost/crystal sounds without external assets."""
from pathlib import Path
import wave
import numpy as np
ROOT=Path(__file__).resolve().parents[2]/'Assets/_PirateLegends/Frostbound/Audio'
ROOT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(7217)
rate=44100
for name,duration,kind in [('FrostCharge',1.3,'charge'),('GlacialSlam',1.4,'impact'),('LanceRelease',.42,'release'),('BowCharge',1.6,'bow'),('ArrowRelease',.55,'release'),('CrystalBurst',1.1,'impact')]:
    t=np.arange(int(rate*duration))/rate
    noise=rng.uniform(-1,1,len(t))
    bright=noise-np.concatenate(([0],noise[:-1]))
    if kind in ['charge','bow']:
        env=np.sin(np.pi*t/duration)**2
        sound=(np.sin(2*np.pi*(350*t+300*t*t))*.3+np.sin(2*np.pi*1030*t)*.16+bright*.055)*env
        if kind=='bow':sound+=np.sin(2*np.pi*1550*t)*.05*env
    elif kind=='release':
        sound=(bright*.24+np.sin(2*np.pi*(1200*t-600*t*t))*.2)*np.exp(-t*13)
        sound+=np.sin(2*np.pi*95*t)*.2*np.exp(-t*20)
    else:
        sound=bright*.17*np.exp(-t*9)+np.sin(2*np.pi*(65*t-9*t*t))*.48*np.exp(-t*7)
        for frequency in [780,1170,1830,2510,3370]:
            delay=rng.uniform(0,.12)
            sound+=.07*np.sin(2*np.pi*frequency*t)*np.exp(-np.maximum(0,t-delay)*rng.uniform(6,11))*(t>=delay)
    fade=np.minimum(t/.005,1)*np.minimum((duration-t)/.03,1)
    sound=np.tanh(sound*fade*.8)
    with wave.open(str(ROOT/(name+'.wav')),'wb') as out:
        out.setnchannels(1);out.setsampwidth(2);out.setframerate(rate)
        out.writeframes((sound*32767).astype('<i2').tobytes())
    print(name)
