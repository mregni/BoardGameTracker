import * as Tooltip from "@radix-ui/react-tooltip";
import { cx } from "class-variance-authority";
import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import type { Badge } from "@/models";

interface Props {
	badge: Badge;
	lockedLabel?: string;
}

export const BgtAchievementIcon = (props: Props) => {
	const { badge, lockedLabel } = props;
	const { t } = useTranslation("badges");
	const [open, setOpen] = useState(false);
	const openAtPress = useRef<boolean | null>(null);

	return (
		<Tooltip.Provider>
			<Tooltip.Root open={open} onOpenChange={setOpen}>
				<Tooltip.Trigger asChild>
					<button
						type="button"
						className="block rounded-md focus-visible:outline-2 focus-visible:outline-primary"
						onPointerDown={() => {
							openAtPress.current = open;
						}}
						onClick={(event) => {
							event.preventDefault();
							const wasOpen = openAtPress.current ?? open;
							openAtPress.current = null;
							setOpen(!wasOpen);
						}}
					>
						<img
							src={`/images/badges/${badge.image}`}
							alt={lockedLabel ? `${t(badge.titleKey)}, ${lockedLabel}` : t(badge.titleKey)}
							className={cx("h-10 aspect-square", lockedLabel && "opacity-40 grayscale")}
						/>
					</button>
				</Tooltip.Trigger>
				<Tooltip.Portal>
					<Tooltip.Content
						className="select-none rounded-sm bg-card-black border-card-border border-2 border-solid px-[15px] py-2.5 text-[15px] leading-none will-change-[transform,opacity] data-[state=delayed-open]:data-[side=bottom]:animate-slide-up-and-fade data-[state=delayed-open]:data-[side=left]:animate-slide-right-and-fade data-[state=delayed-open]:data-[side=right]:animate-slide-left-and-fade data-[state=delayed-open]:data-[side=top]:animate-slide-down-and-fade"
						sideOffset={5}
					>
						<div className="flex flex-col justify-center">
							<div className="font-bold">{t(`${badge.titleKey}`)}</div>
							<div className="text-xs">{t(`${badge.descriptionKey}`)}</div>
							{lockedLabel && <div className="mt-1 text-xs text-white/60">{lockedLabel}</div>}
						</div>
					</Tooltip.Content>
				</Tooltip.Portal>
			</Tooltip.Root>
		</Tooltip.Provider>
	);
};
