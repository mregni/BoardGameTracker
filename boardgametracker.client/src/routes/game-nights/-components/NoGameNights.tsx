import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	filter: "all" | "upcoming" | "past";
	onCreateClick: () => void;
}

export const NoGameNights = (props: Props) => {
	const { filter, onCreateClick } = props;
	const { t } = useTranslation("game-nights");

	const getMessage = () => {
		switch (filter) {
			case "upcoming":
				return t("empty.upcoming");
			case "past":
				return t("empty.past");
			default:
				return t("empty.all");
		}
	};

	return (
		<div className="text-center py-16">
			<BgtText size="5" weight="bold" className="mb-2">
				{t("empty.title")}
			</BgtText>
			<BgtText size="3" color="gray" className="mb-6">
				{getMessage()}
			</BgtText>
			<BgtButton variant="primary" size="3" onClick={onCreateClick}>
				{t("create-first")}
			</BgtButton>
		</div>
	);
};
