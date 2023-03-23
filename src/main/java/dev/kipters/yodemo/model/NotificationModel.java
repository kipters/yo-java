package dev.kipters.yodemo.model;

import com.google.gson.annotations.SerializedName;

public record NotificationModel(
    @SerializedName("Sender") String sender,
    @SerializedName("Target") String target
    ) {

}
