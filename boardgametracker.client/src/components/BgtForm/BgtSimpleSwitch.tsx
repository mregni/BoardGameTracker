import * as Switch from "@radix-ui/react-switch";
import { cx } from "class-variance-authority";
import { useId } from "react";

interface Props {
	label: string;
	disabled?: boolean;
	className?: string;
	value: boolean;
	onChange: (value: boolean) => void;
}

export const BgtSimpleSwitch = (props: Props) => {
	const { label, disabled = false, className, value, onChange } = props;
	const id = useId();

	return (
		<div className={cx(className, disabled && "text-gray-500")}>
			<label htmlFor={id} className="text-[16px]/[24px]">
				<div className="flex gap-2">
					<Switch.Root
						id={id}
						onCheckedChange={onChange}
						disabled={disabled}
						checked={value}
						className="w-[42px] h-[21px] rounded-full relative data-disabled:bg-slate-600 data-[state=checked]:bg-primary outline-hidden cursor-defaul bg-slate-500"
					>
						<Switch.Thumb className="block w-[21px] h-[21px] -left-[2px] top-0 absolute bg-white rounded-full transition-transform duration-100 translate-x-0.5 will-change-transform data-[state=checked]:translate-x-[23px]" />
					</Switch.Root>
					{label}
				</div>
			</label>
		</div>
	);
};
