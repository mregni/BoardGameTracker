import type { ReactNode } from "react";

interface LinkProps {
	children?: ReactNode;
	to: string;
	params?: Record<string, string | number>;
	className?: string;
	title?: string;
}

export const RouterLinkMock = ({ children, to, params, className, title }: LinkProps) => {
	const href = Object.entries(params ?? {}).reduce((path, [key, value]) => path.replace(`$${key}`, String(value)), to);
	return (
		<a href={href} className={className} title={title}>
			{children}
		</a>
	);
};
