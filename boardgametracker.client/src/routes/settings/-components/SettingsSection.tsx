import type { ReactNode } from "react";

import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	title: string;
	description?: string;
	aside?: ReactNode;
	children: ReactNode;
}

export const SettingsSection = ({ title, description, aside, children }: Props) => {
	if (!aside) {
		return (
			<BgtCard title={title} description={description}>
				<div className="space-y-4">{children}</div>
			</BgtCard>
		);
	}

	return (
		<BgtCard className="@container">
			<div className="mb-6 flex flex-col gap-3 @2xl:flex-row @2xl:items-center @2xl:justify-between @2xl:gap-6">
				<div className="min-w-0">
					<h2 className="text-white tracking-wide">{title}</h2>
					{description && (
						<BgtText size="2" color="white" opacity={50}>
							{description}
						</BgtText>
					)}
				</div>
				<div className="shrink-0">{aside}</div>
			</div>
			<div className="space-y-4">{children}</div>
		</BgtCard>
	);
};
