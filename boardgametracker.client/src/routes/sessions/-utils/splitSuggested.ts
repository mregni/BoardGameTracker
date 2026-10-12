export const splitSuggested = <T extends { id: number }>(items: T[], suggestedIds: number[]) => {
	const suggested = suggestedIds.map((id) => items.find((x) => x.id === id)).filter((x): x is T => x !== undefined);
	return { suggested, rest: items.filter((x) => !suggested.includes(x)) };
};
