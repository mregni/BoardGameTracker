import * as Tooltip from "@radix-ui/react-tooltip";
import { useId } from "react";
import BgtButton from "@/components/BgtButton/BgtButton";

interface Props {
	label: string;
	hint: string;
}

export const DisabledActionHint = ({ label, hint }: Props) => {
	const hintId = useId();

	return (
		<Tooltip.Provider delayDuration={150}>
			<Tooltip.Root>
				<Tooltip.Trigger asChild>
					<span className="inline-flex cursor-not-allowed" title={hint}>
						<BgtButton variant="cancel" size="1" disabled aria-describedby={hintId} className="pointer-events-none">
							{label}
						</BgtButton>
						<span id={hintId} className="sr-only">
							{hint}
						</span>
					</span>
				</Tooltip.Trigger>
				<Tooltip.Portal>
					<Tooltip.Content
						sideOffset={5}
						className="max-w-xs select-none rounded-md border-2 border-card-border bg-card-black px-3 py-2 text-xs text-white shadow-lg"
					>
						{hint}
						<Tooltip.Arrow className="fill-card-black" />
					</Tooltip.Content>
				</Tooltip.Portal>
			</Tooltip.Root>
		</Tooltip.Provider>
	);
};
