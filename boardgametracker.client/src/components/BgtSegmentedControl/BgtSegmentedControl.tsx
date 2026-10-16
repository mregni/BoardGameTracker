import { cva, cx } from "class-variance-authority";

const optionVariants = cva(
	"cursor-pointer rounded-lg border font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-card-value",
	{
		variants: {
			size: {
				sm: "px-2.5 py-1 text-xs",
				default: "px-3 py-1.5 text-sm",
			},
			selected: {
				true: "border-primary bg-primary/15 text-white",
				false: "border-transparent text-gray-400 hover:text-white",
			},
		},
		defaultVariants: {
			size: "default",
			selected: false,
		},
	},
);

export interface BgtSegmentedOption<T extends string> {
	value: T;
	label: string;
}

interface Props<T extends string> {
	options: BgtSegmentedOption<T>[];
	value: T;
	onChange: (value: T) => void;
	"aria-label": string;
	size?: "sm" | "default";
	className?: string;
}

export const BgtSegmentedControl = <T extends string>(props: Props<T>) => {
	const { options, value, onChange, size, className, "aria-label": ariaLabel } = props;

	return (
		<fieldset
			className={cx(
				"inline-flex min-w-0 flex-wrap gap-0.5 rounded-[10px] border border-card-border bg-card p-[3px]",
				className,
			)}
		>
			<legend className="sr-only">{ariaLabel}</legend>
			{options.map((option) => (
				<button
					key={option.value}
					type="button"
					aria-pressed={option.value === value}
					onClick={() => onChange(option.value)}
					className={optionVariants({ size, selected: option.value === value })}
				>
					{option.label}
				</button>
			))}
		</fieldset>
	);
};
