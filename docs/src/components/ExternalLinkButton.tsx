import { ExternalLink } from "lucide-react";

interface ExternalLinkButtonProps {
	href: string;
	icon?: React.ComponentType<{ className?: string }>;
	children: React.ReactNode;
	variant?: "primary" | "secondary";
	className?: string;
}

export default function ExternalLinkButton({
	href,
	icon: Icon,
	children,
	variant = "primary",
	className = "",
}: ExternalLinkButtonProps) {
	const baseStyles =
		"inline-flex items-center gap-2 px-6 py-3 rounded-lg transition-all font-medium group";

	const variantStyles = {
		primary: "bg-purple-600 hover:bg-purple-500 text-white",
		secondary:
			"bg-white/10 hover:bg-white/20 text-white border border-white/20 hover:border-white/30",
	};

	return (
		<a
			href={href}
			target="_blank"
			rel="noopener noreferrer"
			className={`${baseStyles} ${variantStyles[variant]} ${className}`}
		>
			{Icon && <Icon className="w-5 h-5" />}
			<span>{children}</span>
			<ExternalLink className="w-4 h-4 opacity-60 group-hover:opacity-100 transition-opacity" />
		</a>
	);
}
