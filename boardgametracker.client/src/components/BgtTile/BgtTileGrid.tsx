import type { HTMLAttributes } from "react";

export const BgtTileGrid = ({ children, ...props }: HTMLAttributes<HTMLDivElement>) => {
	return (
		<div className="grid grid-cols-1 gap-2 sm:grid-cols-[repeat(auto-fill,minmax(240px,1fr))] sm:gap-3" {...props}>
			{children}
		</div>
	);
};
