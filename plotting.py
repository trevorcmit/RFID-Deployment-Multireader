import sys
import matplotlib.pyplot as plt
import csv
from datetime import datetime

# fi = open('tags_part1.csv', 'r')
# data = fi.read()
# fi.close()
# fo = open('mynew1.csv', 'w')
# fo.write(data.replace('\x00', ''))
# fo.close()

# fi = open('demo.csv', 'r')
# data = fi.read()
# fi.close()
# fo = open('nose1.csv', 'w')
# fo.write(data.replace('\x00', ''))
# fo.close()

EPCs = {
    "E282403E000207D6F9773F3C": 'Left Quad',
    "E282403E000207D6F9772FB3": 'Right Glute',
    "E282403E000207D6F97783A5": 'Right Quad',
    "E282403E000207D6F9777325": 'Left Calf',
    "E282403E000207D6F9779FC6": 'Right Calf',
    'ÿþE282403E000207D6F9772CAE': 'Left Glute',
    'E282403E000207D6F9772CAE': 'Left Glute'

    # "477265794861743030303031": 'Trevor Hat',
    # "426C61636B48617446726F6E": 'Roee Hat',
    # "4761627269656C4D61736B5F": 'Gabriel Mask',
    # "426C75654C65667441524D5F": 'Gabriel Left Arm',
    # "426C7565526967687441524D": 'Gabriel Right Arm',
    # "4761627269656C4C536F636B": 'Gabriel Left Sock',
    # "4761627269656C52536F636B": "Gabriel Right Sock",
    # "476162526967687450616C6D": "Left Roee Glove",
    # "57656E64795269675468756D": "Right Roee Glove",
    # "547265766F724C656650616C": "Left Trevor Glove",
    # "57656E64794C656650616C6D": "Right Trevor Glove"
}

start_temp = {
    "E282403E000207D6F9773F3C": 0,
    "E282403E000207D6F9772FB3": 0,
    "E282403E000207D6F97783A5": 0,
    "E282403E000207D6F9777325": 0,
    "E282403E000207D6F9779FC6": 0,
    'ÿþE282403E000207D6F9772CAE': 0,
    'E282403E000207D6F9772CAE': 0
}

right = {
    "E282403E000207D6F9772FB3": 'Right Glute',
    "E282403E000207D6F97783A5": 'Right Quad',
    "E282403E000207D6F9779FC6": 'Right Calf',
}

left = {
    "E282403E000207D6F9773F3C": 'Left Quad',
    "E282403E000207D6F9777325": 'Left Calf',
    'ÿþE282403E000207D6F9772CAE': 'Left Glute',
    'E282403E000207D6F9772CAE': 'Left Glute'
}

data = dict()

data2 = dict()

if True:
    with open('mynew1.csv', 'r') as file:
        reader = csv.reader(file)
        labels = next(reader)           # Get the first row which are the EPC's

        for n in labels:
            data[n] = [[], []]          # Setup data dictionary for nested Time/Data lists

        for row in reader:              # Begin data iteration
            for j in range(len(row)):   # Iterate over row in table
                if row[j] == "": 
                    break               # If it's an empty entry, skip iteration
                tokens = row[j].split(",")
                time = datetime.strptime(tokens[0][1:], "%H:%M:%S")
                temp = float(tokens[1][:-1])
                data[labels[j]][0].append(time)
                data[labels[j]][1].append(temp)

for tag in data.keys():
    start_temp[tag] = sum(data[tag][1]) / len(data[tag][1])



if True:
    with open('mynew2.csv', 'r') as file:
        reader = csv.reader(file)
        labels = next(reader)

        for n in labels:
            if n not in data.keys():
                data[n] = [[],[]]
            
            if n not in data2.keys():
                data2[n] = [[], []]

        for row in reader:              # Begin data iteration
            for j in range(len(row)):   # Iterate over row in table
                if row[j] == "": 
                    break               # If it's an empty entry, skip iteration
                tokens = row[j].split(",")
                time = datetime.strptime(tokens[0][1:], "%H:%M:%S")

                print(tokens[0][1:])
                temp = float(tokens[1][:-1])
                data[labels[j]][0].append(time)
                data[labels[j]][1].append(temp)

                data2[labels[j]][0].append(time)
                data2[labels[j]][1].append(temp)

for tag in data2.keys():
    for i in range(len(data2[tag][1])):
        try:
            data2[tag][1][i] -= start_temp[tag]
        except:
            pass

plt.plot()

plt.plot(
    [datetime.strptime("17:47:55", "%H:%M:%S"),datetime.strptime("17:57:22", "%H:%M:%S")],
    [0, 0],
    color="black"
)

for key, value in data2.items(): 
    if key in EPCs:
        plt.plot(value[0], value[1], label=EPCs[key], marker='o')

plt.xlabel("Timestamp")
plt.xticks(rotation = 45)
plt.ylabel("Temp (Celsius)")
plt.title('Delta from Starting (At Rest) Temperature')
plt.legend()
plt.show()