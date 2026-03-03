import * as Switch from "@radix-ui/react-switch";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useCallback } from "react";

import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	field: AnyFieldApi;
	label: string;
	description: string;
	disabled?: boolean;
}

export const SettingsToggle = ({ field, label, description, disabled = false }: Props) => {
	const checked = Boolean(field.state.value);

	const handleCheckedChange = useCallback(
		(value: boolean) => {
			field.handleChange(value);
		},
		[field],
	);

	return (
		<div
			className={`flex items-center justify-between p-3 bg-background rounded-lg border border-white/10 gap-3 ${disabled ? "opacity-50 cursor-not-allowed" : ""}`}
		>
			<Switch.Root
				onCheckedChange={handleCheckedChange}
				disabled={disabled}
				checked={checked}
				className={`relative inline-flex h-7 w-12 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-background ${checked ? "bg-primary" : "bg-white/10"} ${disabled ? "cursor-not-allowed" : "cursor-pointer"}`}
			>
				<Switch.Thumb
					className={`inline-block h-5 w-5 transform rounded-full bg-white transition-transform ${checked ? "translate-x-6" : "translate-x-1"}`}
				/>
			</Switch.Root>
			<div className="flex-1 pr-3">
				<BgtText size="2" weight="medium" color="white">
					{label}
				</BgtText>
				<BgtText size="1" color="white" opacity={50}>
					{description}
				</BgtText>
			</div>
		</div>
	);
};
