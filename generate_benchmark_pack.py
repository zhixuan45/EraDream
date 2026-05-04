import json

pack = {
    "Id": "benchmark_pack",
    "Version": "1.0",
    "Items": []
}

for i in range(10000):
    item_type = "Consumable" if i % 10 != 0 else "Permanent"
    pack["Items"].append({
        "id": f"item_{i}",
        "name": f"Item {i}",
        "description": "Benchmark item",
        "icon": "",
        "type": item_type,
        "max_stack": 99,
        "duration_turns": 0,
        "price": 10
    })

with open("benchmark.behavior.json", "w") as f:
    json.dump(pack, f, indent=4)
