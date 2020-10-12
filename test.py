import numpy as np
import matplotlib
import matplotlib.pyplot as plt

from tensorflow.keras.layers import Dense, Input
from tensorflow.keras import Model
import tensorflow as tf

state_input = Input(shape=(1,))
l1 = Dense(128, activation='swish', kernel_initializer='he_uniform')(state_input)
l2 = Dense(128, activation='swish', kernel_initializer='he_uniform')(l1)
l_out = Dense(2, activation='linear', kernel_initializer='glorot_uniform')(l2)

# Vytvor model
model = Model(inputs=state_input, outputs=l_out)

# Skompiluj model
model.compile(optimizer=tf.keras.optimizers.Adam(learning_rate=0.01), loss='mse')

model.summary()

x_train = np.arange(-3.14, 3.14, step=0.09, dtype=np.float32)
y_train = np.sin(x_train) * np.exp(-x_train) + np.random.normal(0.0, 0.2, size=x_train.shape)
y2_train = np.tan(x_train) * np.log(np.abs(x_train)) + np.random.normal(0.0, 0.2, size=x_train.shape)

x_train = x_train[3:-3]
y_train = y_train[3:-3]
y2_train = y2_train[3:-3]
print(x_train.shape)

idx = np.arange(x_train.shape[0])
np.random.shuffle(idx)

x_train = np.expand_dims(x_train[idx], axis=1)
y_train = np.expand_dims(y_train[idx], axis=1)
y2_train = np.expand_dims(y2_train[idx], axis=1)
print(x_train.shape)

model.fit(x_train, np.concatenate([y_train, y2_train], axis=-1), batch_size=16, epochs=1000, verbose=1)

f = open("log_tf.csv", "w")

print('\n')
model.evaluate(x_train, np.concatenate([y_train, y2_train], axis=-1), batch_size=16)

y_nn = model.predict(x_train)

for x, y, y_true, y2_true in zip(x_train, y_nn, y_train, y2_train):
    f.write(f"{x};{y[0]};{y_true};{y[1]};{y2_true}\n")

f.close()

f = open("dataset.csv", "w")

for x, y, y2 in zip(x_train[idx], y_train[idx], y2_train[idx]):
    f.write(f"{x};{y};{y2}\n")

f.close()

fig, ax = plt.subplots()

ax.scatter(x_train, y_train, 10, c="g", alpha=1, marker=r'o',
            label="y_0_true")
ax.scatter(x_train, y2_train, 10, c="r", alpha=1, marker=r'o',
            label="y_1_true")
ax.scatter(x_train, y_nn[:, 0], 10, c="b", alpha=0.5, marker=r'o',
            label="y_0")
ax.scatter(x_train, y_nn[:, 1], 10, c="y", alpha=0.5, marker=r'o',
            label="y_1")

ax.set(xlabel='input', ylabel='output',
       title='')
ax.legend(loc='upper left')
ax.grid()

plt.show()