import * as Tooltip from "@radix-ui/react-tooltip";

interface Props {
	label: string;
	hint: string;
}

export const HeaderWithHint = ({ label, hint }: Props) => (
	<Tooltip.Provider delayDuration={150}>
		<Tooltip.Root>
			<Tooltip.Trigger asChild>
				<span className="inline-flex items-center gap-1 underline decoration-dotted underline-offset-4" title={hint}>
					{label}
					<span className="sr-only">{hint}</span>
				</span>
			</Tooltip.Trigger>
			<Tooltip.Portal>
				<Tooltip.Content
					sideOffset={5}
					className="select-none rounded-md border-2 border-card-border bg-card-black px-3 py-2 text-xs font-normal normal-case text-white shadow-lg"
				>
					{hint}
					<Tooltip.Arrow className="fill-card-black" />
				</Tooltip.Content>
			</Tooltip.Portal>
		</Tooltip.Root>
	</Tooltip.Provider>
);
