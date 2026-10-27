import { cx } from "class-variance-authority";

interface Props {
	className?: string;
}

export const BgtSpinner = ({ className }: Props) => (
	<span
		aria-hidden="true"
		data-testid="bgt-spinner"
		className={cx(
			"inline-block shrink-0 rounded-full border-2 border-current border-t-transparent motion-safe:animate-spin",
			className,
		)}
	/>
);
