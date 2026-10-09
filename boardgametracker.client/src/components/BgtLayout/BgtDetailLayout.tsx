import type { ReactNode } from "react";

interface Props {
	sidebar: ReactNode;
	children: ReactNode;
}

export const BgtDetailLayout = (props: Props) => {
	const { sidebar, children } = props;

	return (
		<div className="grid grid-cols-1 gap-3 xl:gap-6 lg:grid-cols-[18rem_minmax(0,1fr)] 2xl:grid-cols-[20rem_minmax(0,1fr)] lg:items-start">
			<aside className="contents lg:flex lg:flex-col lg:gap-3 xl:gap-6">{sidebar}</aside>
			<div className="contents lg:flex lg:min-w-0 lg:flex-col lg:gap-3 xl:gap-6">{children}</div>
		</div>
	);
};
