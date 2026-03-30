import { Link } from "@tanstack/react-router";

interface NavLinkProps {
	to: string;
	label: string;
}

export const NavLink = ({ to, label }: Readonly<NavLinkProps>) => {
	return (
		<Link
			to={to}
			className="text-slate-300 hover:text-white transition-colors"
		>
			{label}
		</Link>
	);
};
