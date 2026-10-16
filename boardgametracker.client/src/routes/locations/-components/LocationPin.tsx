import MapPinIcon from "@/assets/icons/map-pin.svg?react";

export const LocationPin = () => {
	return (
		<span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-primary/20 text-primary">
			<MapPinIcon aria-hidden="true" className="size-5" />
		</span>
	);
};
