import { useEffect } from "react";
import { create } from "zustand";

interface BottomNavState {
	hideRequests: number;
	requestHide: () => void;
	releaseHide: () => void;
}

export const useBottomNavStore = create<BottomNavState>((set) => ({
	hideRequests: 0,
	requestHide: () => set((state) => ({ hideRequests: state.hideRequests + 1 })),
	releaseHide: () => set((state) => ({ hideRequests: Math.max(0, state.hideRequests - 1) })),
}));

export const useIsBottomNavHidden = () => useBottomNavStore((state) => state.hideRequests > 0);

export const useHideBottomNav = (hide = true) => {
	const requestHide = useBottomNavStore((state) => state.requestHide);
	const releaseHide = useBottomNavStore((state) => state.releaseHide);

	useEffect(() => {
		if (!hide) return;
		requestHide();
		return releaseHide;
	}, [hide, requestHide, releaseHide]);
};
