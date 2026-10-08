"""Sound recipes. Each module registers its sounds with @recipe(name, description, loop=...)."""
from __future__ import annotations

REGISTRY: dict[str, dict] = {}


def recipe(name: str, description: str, loop: bool = False):
    def register(build):
        REGISTRY[name] = {"build": build, "description": description, "loop": loop}
        return build
    return register
