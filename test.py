import numpy as np
import matplotlib
import matplotlib.pyplot as plt

# bez sumu: avgErr.QNet[Elf] = 0.005627045
# so sumom: 

x_train = np.arange(-3.14, 3.14, step=0.09, dtype=np.float32)
y_train = np.sin(x_train) * np.exp(-x_train) + np.random.normal(0.01, size=x_train.shape)

x_train = x_train[3:-3]
y_train = y_train[3:-3]
print(x_train.shape)

f = open("dataset.csv", "w")

idx = np.arange(x_train.shape[0])
np.random.shuffle(idx)

for x, y in zip(x_train[idx], y_train[idx]):
    f.write(f"{x};{y}\n")

f.close()

fig, ax = plt.subplots()
ax.plot(x_train, y_train)

ax.set(xlabel='time (s)', ylabel='voltage (mV)',
       title='About as simple as it gets, folks')
ax.grid()

plt.show()